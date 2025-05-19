using System.Diagnostics;
using System.Xml.Linq;
using GroupProject.Model.LogicModel;
using GroupProject.Model.Utilities;
using GroupProject.Services;
using GroupProject.ViewModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace GroupProject.View;

public partial class PuzzlePage : ContentPage
{
    private readonly List<Connection> _connections = new();

    private Dictionary<int, CardView> _cardMap = new();

    // Dictionary to keep track of occupied areas on the canvas (efficiently due to hashing)
    private readonly Dictionary<long, HashSet<int>> _occupiedAreas = new();

    private CancellationTokenSource? _dragEndCts;

    // Wiring state
    private bool _isDrawingWire;

    // ID generator
    private int _nextId = 1;
    private BoxView? _tempDot;
    private Line? _tempWire;
    private BoxView? _wireOverlay;
    private int _wireSrcId;
    private double _wireStartX, _wireStartY;

    public PuzzlePage()
    {
        InitializeComponent();

        foreach (var g in Enum.GetValues<GateTypeEnum>())
        {
            var button = new Button
            {
                Text = g.ToString(),
                AutomationId = $"AddGateButton_{g}"
            };
            button.Clicked += (_, _) => { AddGate(g); };
            Sidebar.Children.Add(button);
        }

        SizeChanged += (_, _) => UpdateCanvasSize();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = new PuzzleViewModel();
        LoadInitialCanvas();
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (BindingContext is not PuzzleViewModel vm) return;

        var userEmail = Preferences.Get("UserEmail", null);
        if (string.IsNullOrEmpty(userEmail))
        {
            await DisplayAlert("Error", "User email not found. Please log in.", "OK");
            return;
        }

        var userId = await AuthService.GetUserIdByEmailAsync(userEmail);
        var userInput = await DisplayPromptAsync("Save Puzzle", "Enter puzzle name:", "OK", "Cancel", "Puzzle Name");

        if (!string.IsNullOrWhiteSpace(userInput))
            await vm.SaveAsync(userId, userInput);
    }

    private void Clear_Clicked(object s, EventArgs e)
    {
        if (BindingContext is PuzzleViewModel vm)
            vm.ClearState();

        Canvas.Children.Clear();
        _occupiedAreas.Clear();
        _cardMap.Clear();
        _connections.Clear();

        DisplayAlert("State Cleared", "All cards and connections have been removed.", "OK");
    }

    private CardView CreateCardView(int id, GateTypeEnum type, double x, double y, bool enableOutputTap)
    {
        if (BindingContext is not PuzzleViewModel pvm)
            throw new InvalidOperationException("BindingContext is not a PuzzleViewModel");
        var vm = new CardViewModel(pvm.GetXmlStateService(), id, type)
        {
            X = x,
            Y = y
        };

        var cv = new CardView { BindingContext = vm, AutomationId = $"CardView_{id}" };

        cv.DeleteRequested += Card_DeleteRequested;
        cv.PositionChanged += OnCardMoved;

        if (enableOutputTap)
            cv.OutputPortTapped += OnOutTapped;

        var rect = new Rect(x, y, 120, 80);
        AbsoluteLayout.SetLayoutBounds(cv, rect);
        Canvas.Children.Add(cv);

        _cardMap[id] = cv;
        var key = MathHelper.LongHash(MathHelper.PositionToAreaCoord(x), MathHelper.PositionToAreaCoord(y));

        ForEachRectangleAreaKey(rect, k =>
        {
            if (!_occupiedAreas.TryGetValue(key, out var set))
                _occupiedAreas[key] = set = new HashSet<int>();

            set.Add(id);
        });

        return cv;
    }

    private void BuildConnectionLine(int fromId, int toId, int inputIndex, Point start, Point end)
    {
        var connectionWire = new Line
        {
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
            X1 = start.X, Y1 = start.Y,
            X2 = end.X, Y2 = end.Y,
            InputTransparent = true
        };

        var connection = new Connection
        {
            SourceCardId = fromId,
            TargetCardId = toId,
            TargetInputIndex = inputIndex,
            LineShape = connectionWire
        };

        var hitArea = new ContentView
        {
            BackgroundColor = Colors.Transparent,
            InputTransparent = false
        };

        const double margin = 10;
        var minX = Math.Min(start.X, end.X) - margin;
        var minY = Math.Min(start.Y, end.Y) - margin;
        var width = Math.Abs(end.X - start.X) + margin * 2;
        var height = Math.Abs(end.Y - start.Y) + margin * 2;

        AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

        var tapRecognizer = new TapGestureRecognizer();
        tapRecognizer.Tapped += (_, _) => OnConnectionTapped(connection);
        hitArea.GestureRecognizers.Add(tapRecognizer);

        Canvas.Children.Add(connectionWire);
        Canvas.Children.Add(hitArea);

        connection.HitArea = hitArea;
        _connections.Add(connection);
    }

    private void RestoreConnection(int fromId, int toId, int inputIndex)
    {
        var sourceCv = _cardMap[fromId];
        var targetCv = _cardMap[toId];

        var sb = AbsoluteLayout.GetLayoutBounds(sourceCv);
        var tb = AbsoluteLayout.GetLayoutBounds(targetCv);

        var start = new Point(sb.X + sb.Width, sb.Y + sb.Height / 2);
        var end = new Point(
            tb.X,
            tb.Y + tb.Height * (inputIndex == 1 ? 0.25 : 0.75)
        );

        BuildConnectionLine(fromId, toId, inputIndex, start, end);
    }

    private void Simulate_Clicked(object s, EventArgs e)
    {
        try
        {
            if (BindingContext is not PuzzleViewModel vm)
                return;

            var (success, newCardMap) = vm.ReshuffleIds(_cardMap, _connections);
            if (!success)
            {
                DisplayAlert("Error", "Reshuffling failed. Please check your connections.", "OK");
                return;
            }

            _cardMap = newCardMap;

            var outputs = vm.EvaluateOutputs(); // <-- CRASH LIKELY HERE

            foreach (var child in Canvas.Children)
            {
                if (child is CardView cardView && cardView.BindingContext is CardViewModel viewModel)
                {
                    if (viewModel.GateType == GateTypeEnum.Output)
                    {
                        var match = outputs.FirstOrDefault(o => o.OutputCardId == viewModel.Id);
                        cardView.OutputValueLabel.Text = match != default ? (match.Value ? "1" : "0") : "";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("Simulation Error", ex.ToString(), "OK");
        }
    }

    private void AddGate(GateTypeEnum gateType)
    {
        if (BindingContext is not PuzzleViewModel vm)
            return;

        var existingIds = vm.GetAllIds();
        var id = _nextId;
        while (existingIds.Contains(id)) id++;
        _nextId = id + 1;

        var spawnStart = new Point(50, 50);
        var position = GetNearestFreeSpot(spawnStart, new Size(120, 80));

        // Create and place the card
        CreateCardView(id, gateType, position.X, position.Y, gateType != GateTypeEnum.Output);

        // Add to XML
        vm.AddGate(id, gateType, position.X, position.Y);

        UpdateCanvasSize();
    }

    private void Card_DeleteRequested(object? sender, EventArgs e)
    {
        var cardView = sender as CardView;

        if (cardView == null)
            return;

        // Retrieve the card's id from its BindingContext (view model)
        var vm = cardView.BindingContext as CardViewModel;
        if (vm == null)
            return;

        var cardId = vm.Id;
        // Remove the card from the canvas
        Canvas.Children.Remove(cardView);
        var bounds = AbsoluteLayout.GetLayoutBounds(cardView);
        var rect = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        ForEachRectangleAreaKey(rect, key =>
        {
            if (_occupiedAreas.TryGetValue(key, out var set))
            {
                set.Remove(cardId);
                if (set.Count == 0)
                    _occupiedAreas.Remove(key);
            }
        });

        _cardMap.Remove(cardId);

        // Also remove any connections that reference this card
        var connectionsToRemove = _connections
            .Where(c => c.SourceCardId == cardId || c.TargetCardId == cardId)
            .ToList();
        foreach (var conn in connectionsToRemove)
        {
            Canvas.Children.Remove(conn.LineShape);
            _connections.Remove(conn);
        }

        UpdateCanvasSize();
    }

    private Point GetNearestFreeSpot(Point start, Size gateSize)
    {
        const double step = 10;

        // 1) Exponentially expand until we find at least one free spot on the ring
        double lowRadius = 0;
        double highRadius = step;
        while (!ExistsFreeOnRing(start, gateSize, highRadius, step))
        {
            lowRadius = highRadius;
            highRadius *= 2;
        }

        // 2) Binary-search that interval down to within one “step”
        while (highRadius - lowRadius > step)
        {
            double mid = (lowRadius + highRadius) / 2;
            if (ExistsFreeOnRing(start, gateSize, mid, step))
                highRadius = mid;
            else
                lowRadius = mid;
        }

        // 3) Final local scan in the square [0..highRadius]×[0..highRadius]
        return FindNearestInSquare(start, gateSize, highRadius, step);
    }

    private bool ExistsFreeOnRing(Point center, Size gateSize, double radius, double step)
    {
        // Top and bottom edges of the ring:
        for (double dx = 0; dx <= radius; dx += step)
        {
            foreach (var dy in new[] { 0d, radius })
            {
                var r = new Rect(center.X + dx, center.Y + dy, gateSize.Width, gateSize.Height);
                if (!IsOverlapping(r)) return true;
            }
        }

        // Left and right edges of the ring (excluding corners already checked):
        for (double dy = step; dy < radius; dy += step)
        {
            foreach (var dx in new[] { 0d, radius })
            {
                var r = new Rect(center.X + dx, center.Y + dy, gateSize.Width, gateSize.Height);
                if (!IsOverlapping(r)) return true;
            }
        }

        return false;
    }

    private Point FindNearestInSquare(Point center, Size gateSize, double radius, double step)
    {
        Point best = new Point(center.X, center.Y);
        double bestDist2 = double.MaxValue;

        for (double dx = 0; dx <= radius; dx += step)
        for (double dy = 0; dy <= radius; dy += step)
        {
            var r = new Rect(center.X + dx, center.Y + dy, gateSize.Width, gateSize.Height);
            if (!IsOverlapping(r))
            {
                double d2 = dx * dx + dy * dy;
                if (d2 < bestDist2)
                {
                    bestDist2 = d2;
                    best = new Point(center.X + dx, center.Y + dy);
                }
            }
        }

        return best;
    }

    private void ForEachRectangleAreaKey(Rect rect, Action<long> consumer)
    {
        consumer(MathHelper.LongHash(
            MathHelper.PositionToAreaCoord(rect.X),
            MathHelper.PositionToAreaCoord(rect.Y)));

        consumer(MathHelper.LongHash(
            MathHelper.PositionToAreaCoord(rect.Right),
            MathHelper.PositionToAreaCoord(rect.Bottom)));

        consumer(MathHelper.LongHash(
            MathHelper.PositionToAreaCoord(rect.X),
            MathHelper.PositionToAreaCoord(rect.Bottom)));

        consumer(MathHelper.LongHash(
            MathHelper.PositionToAreaCoord(rect.Right),
            MathHelper.PositionToAreaCoord(rect.Y)));
    }

    private bool IsOverlapping(Rect candidateRect)
    {
        var expandedCandidateRect = new Rect(candidateRect.X - 10, candidateRect.Y - 10,
            candidateRect.Width + 20, candidateRect.Height + 20);

        // Check if the candidate rectangle overlaps with any existing rectangles
        bool intersects = false;

        ForEachRectangleAreaKey(expandedCandidateRect, key =>
        {
            var coords = MathHelper.Unhash(key);
            var x = coords[0];
            var y = coords[1];

            for (int dX = -1; dX <= 1; dX++)
            for (int dY = -1; dY <= 1; dY++)
            {
                var newKey = MathHelper.LongHash(x + dX, y + dY);
                if (_occupiedAreas.TryGetValue(newKey, out var set))
                {
                    foreach (var id in set)
                    {
                        var cv = _cardMap[id];
                        var bounds = AbsoluteLayout.GetLayoutBounds(cv);
                        var existingRect = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                        if (expandedCandidateRect.IntersectsWith(existingRect))
                        {
                            intersects = true;
                            return;
                        }
                    }
                }
            }
        });

        return intersects;
    }

    private void OnCardMoved(object? s, PositionChangedEventArgs e)
    {
        if (_isDrawingWire) return;

        if (BindingContext is not PuzzleViewModel vm)
            return;

        var cv = s as CardView;
        var id = _cardMap.First(kvp => kvp.Value == cv).Key;

        foreach (var c in _connections.Where(c => c.SourceCardId == id || c.TargetCardId == id))
            UpdateConnectionLine(c);

        var bounds = AbsoluteLayout.GetLayoutBounds(cv);
        var newX = bounds.X;
        var newY = bounds.Y;

        var oldX = e.OldX;
        var oldY = e.OldY;
        var oldRect = new Rect(oldX, oldY, bounds.Width, bounds.Height);
        var newRect = new Rect(newX, newY, bounds.Width, bounds.Height);

        ForEachRectangleAreaKey(oldRect, key =>
        {
            if (_occupiedAreas.TryGetValue(key, out var set))
            {
                set.Remove(id);

                if (set.Count == 0)
                    _occupiedAreas.Remove(key);
            }
        });

        ForEachRectangleAreaKey(newRect, key =>
        {
            if (!_occupiedAreas.TryGetValue(key, out var set))
                _occupiedAreas[key] = set = new HashSet<int>();

            set.Add(id);
        });

        // Cancel previous pending updates to avoid unnecessary writes
        _dragEndCts?.Cancel();
        _dragEndCts = new CancellationTokenSource();

        Task.Delay(500, _dragEndCts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            vm.UpdateCardPosition(id, newX, newY);
        }, TaskScheduler.FromCurrentSynchronizationContext());

        UpdateCanvasSize();
    }

    private void OnOutTapped(object? sender, EventArgs e)
    {
        if (_isDrawingWire || sender is not CardView cv) return;

        var sourceId = _cardMap.First(kvp => kvp.Value == cv).Key;
        var bounds = AbsoluteLayout.GetLayoutBounds(cv);
        var startPoint = new Point(bounds.X + bounds.Width, bounds.Y + bounds.Height / 2);

        BeginWireDrawing(sourceId, startPoint);
    }

    private bool TrySnapConnection(double dropX, double dropY)
    {
        const double snapRadius = 25;

        var rect = new Rect(dropX - snapRadius, dropY - snapRadius, snapRadius * 2, snapRadius * 2);

        var snapped = false;

        ForEachRectangleAreaKey(rect, k =>
        {
            if (_occupiedAreas.TryGetValue(k, out var set))
            {
                foreach (var tgtId in set)
                {
                    var cv = _cardMap[tgtId];
                    var bounds = AbsoluteLayout.GetLayoutBounds(cv);
                    var in1 = new Point(bounds.X, bounds.Y + bounds.Height * 0.25);
                    var in2 = new Point(bounds.X, bounds.Y + bounds.Height * 0.75);

                    if (SnapToInputPort(tgtId, dropX, dropY, snapRadius, in1, 1) ||
                        SnapToInputPort(tgtId, dropX, dropY, snapRadius, in2, 2))
                    {
                        snapped = true;
                        return;
                    }
                }
            }
        });

        return snapped;
    }

    private bool SnapToInputPort(int tgtId, double dropX, double dropY, double snapRadius, Point in1, int portIndex)
    {
        if (BindingContext is not PuzzleViewModel vm)
            return false;

        if (MathHelper.Distance(dropX, dropY, in1.X, in1.Y) >= snapRadius || _tempWire == null)
            return false;
        // Check if input port 1 is available.
        if (!_connections.Any(c => c.TargetCardId == tgtId && c.TargetInputIndex == portIndex))
        {
            // Snap the temporary wire to the input point.
            _tempWire.X2 = in1.X;
            _tempWire.Y2 = in1.Y;

            // Create a new connection using _tempWire.
            var newConnection = new Connection
            {
                SourceCardId = _wireSrcId,
                TargetCardId = tgtId,
                TargetInputIndex = portIndex,
                LineShape = _tempWire
            };

            // Now create and add the invisible hit area.
            var hitArea = new ContentView
            {
                BackgroundColor =
                    Colors.Transparent, // set to a visible color (e.g. Colors.Black) for debugging
                InputTransparent = false // ensure it can receive touch input
            };

            // Calculate the bounding box for the hit area with extra margin for easier tapping.
            const double margin = 10;
            var minX = Math.Min(_tempWire.X1, _tempWire.X2) - margin;
            var minY = Math.Min(_tempWire.Y1, _tempWire.Y2) - margin;
            var width = Math.Abs(_tempWire.X2 - _tempWire.X1) + margin * 2;
            var height = Math.Abs(_tempWire.Y2 - _tempWire.Y1) + margin * 2;
            AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

            // Attach a tap gesture recognizer for bringing up the delete button.
            var tapRecognizer = new TapGestureRecognizer();
            tapRecognizer.Tapped += (_, _) =>
            {
                Debug.WriteLine("Connection hit area tapped!");
                OnConnectionTapped(newConnection);
            };
            hitArea.GestureRecognizers.Add(tapRecognizer);

            // Add the hit area (and the connection line already exists) to the Canvas.
            Canvas.Children.Add(hitArea);

            newConnection.HitArea = hitArea;
            _connections.Add(newConnection);

            // Add connection to xml
            vm.ConnectCards(_wireSrcId, tgtId, portIndex);
            return true;
        }

        return false;
    }

    private void OnWireOverlayPan(object? sender, PanUpdatedEventArgs e)
    {
        if (!_isDrawingWire || _tempWire == null) return;

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                var cx = _wireStartX + e.TotalX;
                var cy = _wireStartY + e.TotalY;

                AbsoluteLayout.SetLayoutBounds(_tempDot, new Rect(cx - 6, cy - 6, 12, 12));

                _tempWire.X2 = cx;
                _tempWire.Y2 = cy;
                break;

            case GestureStatus.Canceled:
            case GestureStatus.Completed:
                var dotBounds = AbsoluteLayout.GetLayoutBounds(_tempDot);
                var dropX = dotBounds.X + dotBounds.Width / 2;
                var dropY = dotBounds.Y + dotBounds.Height / 2;

                bool connected = TrySnapConnection(dropX, dropY);

                ExitDrawingMode(connected);
                break;
        }
    }

    private void ExitDrawingMode(bool connected)
    {
        if (_isDrawingWire)
        {
            if (!connected)
                Canvas.Children.Remove(_tempWire);
            Canvas.Children.Remove(_tempDot);
            Canvas.Children.Remove(_wireOverlay);

            _tempWire = null;
            _tempDot = null;
            _wireOverlay = null;
            _isDrawingWire = false;
        }
    }

    private async void OnConnectionTapped(Connection connection)
    {
        var line = connection.LineShape;

        if (line == null) return;

        // Calculate the centre point of the line.
        var centerX = (line.X1 + line.X2) / 2;
        var centerY = (line.Y1 + line.Y2) / 2;

        // Adjust the centre point: 10 pixels upward.
        centerY -= 10;

        // Create the delete button.
        var deleteButton = new Button
        {
            Text = "X",
            BackgroundColor = Colors.Red,
            TextColor = Colors.White,
            WidthRequest = 30,
            HeightRequest = 30,
            CornerRadius = 15 // Circular button.
        };

        deleteButton.Clicked += (_, _) =>
        {
            // Remove connection from XML
            if (BindingContext is PuzzleViewModel vm)
                vm.GetXmlStateService().SetCardInput(connection.TargetCardId, connection.TargetInputIndex, 0);

            // Remove connection's visual elements.
            if (Canvas.Children.Contains(connection.LineShape))
                Canvas.Children.Remove(connection.LineShape);

            if (Canvas.Children.Contains(connection.HitArea))
                Canvas.Children.Remove(connection.HitArea);

            if (Canvas.Children.Contains(deleteButton))
                Canvas.Children.Remove(deleteButton);

            _connections.Remove(connection);
        };

        // Position the delete button at the center of the line.
        AbsoluteLayout.SetLayoutBounds(deleteButton, new Rect(centerX - 15, centerY - 15, 30, 30));
        Canvas.Children.Add(deleteButton);

        deleteButton.Clicked += (_, _) =>
        {
            if (Canvas.Children.Contains(connection.LineShape))
                Canvas.Children.Remove(connection.LineShape);

            if (Canvas.Children.Contains(connection.HitArea))
                Canvas.Children.Remove(connection.HitArea);

            if (Canvas.Children.Contains(deleteButton))
                Canvas.Children.Remove(deleteButton);
            _connections.Remove(connection);
        };

        // Wait for 1 second, then auto-remove the button if it hasn't been interacted with.
        await Task.Delay(1000);
        if (Canvas.Children.Contains(deleteButton)) Canvas.Children.Remove(deleteButton);
    }

    private Point GetOutputPortPosition(CardView cv)
    {
        var b = AbsoluteLayout.GetLayoutBounds(cv);
        return new Point(b.X + b.Width, b.Y + b.Height / 2);
    }

    private Point GetInputPortPosition(CardView cv, int inputIndex)
    {
        var b = AbsoluteLayout.GetLayoutBounds(cv);
        double offsetY = inputIndex == 1 ? 0.25 : 0.75;
        return new Point(b.X, b.Y + b.Height * offsetY);
    }

    private void UpdateConnectionLine(Connection c)
    {
        if (!_cardMap.TryGetValue(c.SourceCardId, out var source) ||
            !_cardMap.TryGetValue(c.TargetCardId, out var target) ||
            c.LineShape == null)
            return;

        var start = GetOutputPortPosition(source);
        var end = GetInputPortPosition(target, c.TargetInputIndex);

        c.LineShape.X1 = start.X;
        c.LineShape.Y1 = start.Y;
        c.LineShape.X2 = end.X;
        c.LineShape.Y2 = end.Y;
    }

    private void UpdateCanvasSize()
    {
        const double pad = 20;
        if (_cardMap.Count == 0) return;

        var right = _cardMap.Values.Max(cv => AbsoluteLayout.GetLayoutBounds(cv).Right);
        var bottom = _cardMap.Values.Max(cv => AbsoluteLayout.GetLayoutBounds(cv).Bottom);

        Canvas.WidthRequest = Math.Max(right + pad, HorizontalScroll.Width);
        Canvas.HeightRequest = Math.Max(bottom + pad, VerticalScroll.Height);
    }

    private void LoadInitialCanvas()
    {
        Canvas.Children.Clear();
        _cardMap.Clear();
        _occupiedAreas.Clear();
        _connections.Clear();

        if (BindingContext is not PuzzleViewModel vm)
            return;

        // Load and render all cards
        var cards = vm.LoadCards();
        foreach (var card in cards)
        {
            CreateCardView(
                id: card.Id,
                type: card.GateType,
                x: card.X,
                y: card.Y,
                enableOutputTap: card.GateType != GateTypeEnum.Output
            );
        }

        // Load and draw all connections
        var conns = vm.LoadConnections();
        foreach (var conn in conns)
        {
            RestoreConnection(conn.FromId, conn.ToId, conn.TargetInputIndex);
        }

        UpdateCanvasSize();
    }

    private void BeginWireDrawing(int sourceId, Point startPoint)
    {
        _wireSrcId = sourceId;
        _wireStartX = startPoint.X;
        _wireStartY = startPoint.Y;

        _tempWire = new Line
        {
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
            X1 = startPoint.X, Y1 = startPoint.Y,
            X2 = startPoint.X, Y2 = startPoint.Y,
            InputTransparent = true
        };
        Canvas.Children.Add(_tempWire);

        _tempDot = new BoxView
        {
            WidthRequest = 12,
            HeightRequest = 12,
            CornerRadius = 6,
            BackgroundColor = Colors.White
        };
        AbsoluteLayout.SetLayoutBounds(_tempDot,
            new Rect(startPoint.X - 6, startPoint.Y - 6, 12, 12));
        Canvas.Children.Add(_tempDot);

        _wireOverlay = new BoxView
        {
            BackgroundColor = Colors.Transparent
        };
        AbsoluteLayout.SetLayoutFlags(_wireOverlay, AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(_wireOverlay, new Rect(0, 0, 1, 1));

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnWireOverlayPan;
        _wireOverlay.GestureRecognizers.Add(pan);
        Canvas.Children.Add(_wireOverlay);

        _isDrawingWire = true;
    }
}