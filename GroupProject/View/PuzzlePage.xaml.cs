using System.Diagnostics;
using System.Xml.Linq;
using GroupProject.Model.LogicModel;
using GroupProject.Services;
using GroupProject.ViewModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;
using Path = System.IO.Path;

namespace GroupProject.View;

public partial class PuzzlePage : ContentPage
{
    private static readonly FilePickerFileType XmlFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".xml", "application/xml" } },
        { DevicePlatform.MacCatalyst, new[] { "xml" } },
        { DevicePlatform.iOS, new[] { "public.xml" } },
        { DevicePlatform.Android, new[] { "text/xml", "application/xml" } }
    });

    private readonly List<Connection> _connections = new();

    private readonly string statePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");

    private Dictionary<int, CardView> _cardMap = new();
    private double _dotStartX, _dotStartY;

    // ── Card drag ──────────────────────────────────────────────────────

    private CancellationTokenSource _dragEndCts;

    // Wiring state
    private bool _isDrawingWire;

    // ID generator
    private int _nextId = 1;
    private string _stateFilePath = string.Empty;
    private StateParser _stateParser;
    private BoxView _tempDot;
    private Line _tempWire;
    private BoxView _wireOverlay;
    private int _wireSrcId;
    private double _wireStartX, _wireStartY;

    public PuzzlePage()
    {
        InitializeComponent();

        // InitizaliseFilewaterHere Michael

        foreach (var g in Enum.GetValues<GateTypeEnum>())
        {
            var button = new Button
            {
                Text = g.ToString()
            };
            button.Clicked += (s, e) => { AddGate(g.ToString()); };
            Sidebar.Children.Add(button);
        }

        SizeChanged += (_, __) => UpdateCanvasSize();
        _stateParser = new StateParser();

        loadInitialCanvas();
    }

    private async void Save_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_stateFilePath))
        {
            var pick = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Save puzzle as…",
                FileTypes = XmlFileType
            });
            if (pick == null)
                return;
            _stateFilePath = pick.FullPath;
        }

        // Always re-parse the current UI state back into the parser,
        // then save it out to the known path.
        var (ins, gates, outs) = _stateParser.parseCards();
        _stateParser.SaveCards(_stateFilePath, ins, gates, outs);

        await DisplayAlert("Saved", $"Puzzle written to:\n{_stateFilePath}", "OK");
    }

    private async void Load_Clicked(object sender, EventArgs e)
    {
        // 1) Let the user pick an XML file
        var pick = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select puzzle XML" });
        if (pick == null) return;

        // 2) Parse into our single StateParser instance, remember the path
        var xml = await File.ReadAllTextAsync(pick.FullPath);
        _stateParser = new StateParser(XDocument.Parse(xml));
        _stateFilePath = pick.FullPath;
        var (inputs, logicGates, outputs) = _stateParser.parseCards();

        // 3) Clear existing UI
        Canvas.Children.Clear();
        _cardMap.Clear();
        _connections.Clear();
        _nextId = 1;

        // 4) Place input cards in column 0
        const double hSpacing = 200, vSpacing = 120, margin = 20;
        for (var i = 0; i < inputs.Count; i++)
        {
            PlaceCard(inputs[i].Id, "IN",
                margin,
                margin + i * vSpacing);
            _nextId = Math.Max(_nextId, inputs[i].Id + 1);
        }

        // 5) Compute each logic gate's "level" (the max level of its inputs + 1)
        var levels = new Dictionary<int, int>();
        foreach (var ic in inputs)
            levels[ic.Id] = 0;

        bool progress;
        do
        {
            progress = false;
            foreach (var g in logicGates)
            {
                if (levels.ContainsKey(g.Id)) continue;
                if (g.Input1Card == null || g.Input2Card == null) continue;

                var id1 = GetCardId(g.Input1Card);
                var id2 = GetCardId(g.Input2Card);
                if (!levels.TryGetValue(id1, out var l1) ||
                    !levels.TryGetValue(id2, out var l2))
                    continue;

                levels[g.Id] = Math.Max(l1, l2) + 1;
                progress = true;
            }
        } while (progress);

        var maxGateLevel = levels.Values.Where(l => l > 0).DefaultIfEmpty(0).Max();

        // 6) Place gates by level, ordering within each level by barycenter of their inputs
        foreach (var grp in logicGates
                     .Where(g => levels.ContainsKey(g.Id))
                     .GroupBy(g => levels[g.Id])
                     .OrderBy(g => g.Key))
        {
            var level = grp.Key;
            // compute score = average source‐Y for each gate
            var ordered = grp
                .Select(g =>
                {
                    var y1 = AbsoluteLayout.GetLayoutBounds(_cardMap[GetCardId(g.Input1Card)]).Y;
                    var y2 = AbsoluteLayout.GetLayoutBounds(_cardMap[GetCardId(g.Input2Card)]).Y;
                    return (Gate: g, Score: (y1 + y2) / 2.0);
                })
                .OrderBy(x => x.Score)
                .Select(x => x.Gate)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var g = ordered[i];
                PlaceCard(g.Id, g.GateType.ToString(),
                    margin + level * hSpacing,
                    margin + i * vSpacing);
                _nextId = Math.Max(_nextId, g.Id + 1);
            }
        }

        // 7) Place outputs in the final column
        var outLevel = maxGateLevel + 1;
        for (var i = 0; i < outputs.Count; i++)
        {
            var oc = outputs[i];
            PlaceCard(oc.Id, "OUT",
                margin + outLevel * hSpacing,
                margin + i * vSpacing);
            _nextId = Math.Max(_nextId, oc.Id + 1);
        }

        // 8) Rebuild all permanent wires
        //    a) Gate inputs
        foreach (var g in logicGates)
        {
            var src1 = GetCardId(g.Input1Card);
            var src2 = GetCardId(g.Input2Card);
            if (g.Input1Card != null)
                CreateConnection(src1, g.Id, 1);
            if (g.Input2Card != null)
                CreateConnection(src2, g.Id, 2);
        }

        //    b) Outputs
        foreach (var oc in outputs)
        {
            var src = GetCardId(oc.Input1Card);
            if (oc.Input1Card != null)
                CreateConnection(src, oc.Id, 1);
        }

        // 9) Finally, adjust the canvas to fit everything
        UpdateCanvasSize();
    }

    private void Clear_Clicked(object s, EventArgs e)
    {
        var xmlService = new XmlStateService(statePath);

        xmlService.ClearStateFile();

        Canvas.Children.Clear();

        _cardMap.Clear();
        _connections.Clear();

        DisplayAlert("State Cleared", "All cards and connections have been removed.", "OK");
    }

    private void Simulate_Clicked(object s, EventArgs e)
    {
        ReshuffleIds();

        var calculateParser = new StateParser();

        var xmlService = new XmlStateService(statePath);

        var (_, _, outputCards) = calculateParser.parseCards();

        xmlService.PrintStateFile();

        // Loop through all children of the canvas
        foreach (var child in Canvas.Children)
            if (child is CardView cardView && cardView.BindingContext is CardViewModel viewModel)
                // Check if this is an output card
                if (viewModel.GateType.Equals("Output", StringComparison.OrdinalIgnoreCase))
                {
                    // Find the matching outputCard from the list using the id
                    var matchingOutput = outputCards.FirstOrDefault(o => o.Id == viewModel.Id);
                    if (matchingOutput != null)
                        // Update the OutputValueLabel text to "1" if true, or "0" if false
                        cardView.OutputValueLabel.Text = matchingOutput.Output ? "1" : "0";
                }
    }

    // ── AddGate helper ─────────────────────────────────────────────────

    private void AddGate(string gateType)
    {
        var xmlService = new XmlStateService(statePath);

        // Fetch all existing IDs
        var existingIds = xmlService.GetAllIds();

        // Find the next available ID
        var id = _nextId;
        while (existingIds.Contains(id)) id++;
        _nextId = id + 1; // Increment for next addition

        var vm = new CardViewModel(id, gateType);
        var spawnStart = new Point(50, 50);
        var candidate = GetNearestFreeSpot(spawnStart, new Size(120, 80));

        vm.X = candidate.X;
        vm.Y = candidate.Y;

        var cv = new CardView { BindingContext = vm };
        cv.DeleteRequested += Card_DeleteRequested;
        AbsoluteLayout.SetLayoutBounds(cv, new Rect(vm.X, vm.Y, 120, 80));
        Canvas.Children.Add(cv);
        cv.PositionChanged += OnCardMoved;

        if (gateType == "Input")
        {
            cv.OutputPortTapped += OnOutTapped;
            xmlService.AddInputCard(id, false, vm.X, vm.Y);
        }
        else if (gateType == "Output")
        {
            cv.InputPortTapped += OnInTapped;
            xmlService.AddOutputCard(id, 0, vm.X, vm.Y);
        }
        else
        {
            cv.InputPortTapped += OnInTapped;
            cv.OutputPortTapped += OnOutTapped;
            xmlService.AddLogicGateCard(id, gateType, 0, 0, vm.X, vm.Y);
        }

        _cardMap[id] = cv;
        UpdateCanvasSize();
    }

    private void Card_DeleteRequested(object sender, EventArgs e)
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
        // Remove it from your map
        if (_cardMap.ContainsKey(cardId))
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

    private void DeleteConnection(Connection connection)
    {
        Canvas.Children.Remove(connection.LineShape);
        _connections.Remove(connection);
    }

    private Point GetNearestFreeSpot(Point start, Size gateSize)
    {
        double step = 10; // increment in pixels
        var bestDistance = double.MaxValue;
        var bestCandidate = start;

        // Search in a square around the spawn start.
        // Adjust the range if necessary (here, -300 to +300 pixels in both dimensions)
        for (var offsetX = -300; offsetX <= 300; offsetX += (int)step)
        for (var offsetY = -300; offsetY <= 300; offsetY += (int)step)
        {
            var candidate = new Point(start.X + offsetX, start.Y + offsetY);

            if (candidate.X < 10 || candidate.Y < 10)
                continue;

            var candidateRect = new Rect(candidate, gateSize);
            if (!IsOverlapping(candidateRect))
            {
                var distance = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = candidate;
                }
            }
        }

        return bestCandidate;
    }

    private bool IsOverlapping(Rect candidateRect)
    {
        foreach (var card in _cardMap.Values)
        {
            var existingRect = AbsoluteLayout.GetLayoutBounds(card);
            var expandedRect = new Rect(existingRect.X - 10, existingRect.Y - 10, existingRect.Width + 20,
                existingRect.Height + 20);
            if (candidateRect.IntersectsWith(expandedRect))
                return true;
        }

        return false;
    }

    private void OnCardMoved(object s, PositionChangedEventArgs e)
    {
        if (_isDrawingWire) return;

        var cv = (CardView)s;
        var id = _cardMap.First(kvp => kvp.Value == cv).Key;

        foreach (var c in _connections.Where(c => c.SourceCardId == id || c.TargetCardId == id))
            UpdateConnectionLine(c);

        var bounds = AbsoluteLayout.GetLayoutBounds(cv);
        var newX = bounds.X;
        var newY = bounds.Y;

        // Cancel previous pending updates to avoid unnecessary writes
        _dragEndCts?.Cancel();
        _dragEndCts = new CancellationTokenSource();

        Task.Delay(500, _dragEndCts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                var xmlService = new XmlStateService(statePath);
                xmlService.UpdateCardPosition(id, newX, newY);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());

        UpdateCanvasSize();
    }

    // ── Start wiring on output tap ──────────────────────────────────────

    private void OnOutTapped(object sender, EventArgs e)
    {
        if (_isDrawingWire) return;

        // 1) compute origin
        var cv = (CardView)sender;
        _wireSrcId = _cardMap.First(kvp => kvp.Value == cv).Key;
        var b = AbsoluteLayout.GetLayoutBounds(cv);
        _wireStartX = b.X + b.Width;
        _wireStartY = b.Y + b.Height / 2;

        // 2) create line
        _tempWire = new Line
        {
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2,
            X1 = _wireStartX, Y1 = _wireStartY,
            X2 = _wireStartX, Y2 = _wireStartY
        };
        _tempWire.InputTransparent = true;
        Canvas.Children.Add(_tempWire);

        // 3) create dot
        _tempDot = new BoxView
        {
            WidthRequest = 12,
            HeightRequest = 12,
            CornerRadius = 6,
            BackgroundColor = Colors.White
        };
        AbsoluteLayout.SetLayoutBounds(_tempDot,
            new Rect(_wireStartX - 6, _wireStartY - 6, 12, 12));
        Canvas.Children.Add(_tempDot);

        _isDrawingWire = true;

        // 4) spawn an overlay that covers the entire canvas and catches all drags
        _wireOverlay = new BoxView
        {
            BackgroundColor = Colors.Transparent,
        };
        AbsoluteLayout.SetLayoutFlags(_wireOverlay, AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutBounds(_wireOverlay, new Rect(0, 0, 1, 1));

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnWireOverlayPan;
        _wireOverlay.GestureRecognizers.Add(pan);

        Canvas.Children.Add(_wireOverlay);
    }

    private void OnWireOverlayPan(object sender, PanUpdatedEventArgs e)
    {
        var xmlService = new XmlStateService(statePath);

        if (!_isDrawingWire) return;

        if (e.StatusType == GestureStatus.Running)
        {
            // move the dot + update line
            var cx = _wireStartX + e.TotalX;
            var cy = _wireStartY + e.TotalY;

            AbsoluteLayout.SetLayoutBounds(_tempDot,
                new Rect(cx - 6, cy - 6, 12, 12));

            _tempWire.X2 = cx;
            _tempWire.Y2 = cy;
        }
        else if (e.StatusType == GestureStatus.Completed
                 || e.StatusType == GestureStatus.Canceled)
        {
            // 1) Compute final drop point in canvas coords
            // Temp change
            // var dropX = _wireStartX + e.TotalX;
            // var dropY = _wireStartY + e.TotalY;
            var dotBounds = AbsoluteLayout.GetLayoutBounds(_tempDot);
            var dropX = dotBounds.X + dotBounds.Width / 2;
            var dropY = dotBounds.Y + dotBounds.Height / 2;

            const double snapRadius = 25; // px threshold for “over” a port
            var connected = false;

            // 2) Hit-test every card’s two input ports
            foreach (var kvp in _cardMap)
            {
                var tgtId = kvp.Key;
                var cv = kvp.Value;
                var b = AbsoluteLayout.GetLayoutBounds(cv);

                // Input‐1 center (left, upper quarter)
                var in1 = new Point(b.X, b.Y + b.Height * 0.25);
                // Input‐2 center (left, lower quarter)
                var in2 = new Point(b.X, b.Y + b.Height * 0.75);

                // --- For Input Port 1 ---
                if (Distance(dropX, dropY, in1.X, in1.Y) < snapRadius)
                {
                    // Check if input port 1 is available.
                    if (!_connections.Any(c => c.TargetCardId == tgtId && c.TargetInputIndex == 1))
                    {
                        // Snap the temporary wire to the input point.
                        _tempWire.X2 = in1.X;
                        _tempWire.Y2 = in1.Y;

                        // Create a new connection using _tempWire.
                        var newConnection = new Connection
                        {
                            SourceCardId = _wireSrcId,
                            TargetCardId = tgtId,
                            TargetInputIndex = 1,
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
                        double margin = 10;
                        var minX = Math.Min(_tempWire.X1, _tempWire.X2) - margin;
                        var minY = Math.Min(_tempWire.Y1, _tempWire.Y2) - margin;
                        var width = Math.Abs(_tempWire.X2 - _tempWire.X1) + margin * 2;
                        var height = Math.Abs(_tempWire.Y2 - _tempWire.Y1) + margin * 2;
                        AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

                        // Attach a tap gesture recognizer for bringing up the delete button.
                        var tapRecognizer = new TapGestureRecognizer();
                        tapRecognizer.Tapped += (s, e) =>
                        {
                            Debug.WriteLine("Connection hit area tapped!");
                            OnConnectionTapped(newConnection);
                        };
                        hitArea.GestureRecognizers.Add(tapRecognizer);

                        // Add the hit area (and the connection line already exists) to the Canvas.
                        Canvas.Children.Add(hitArea);

                        newConnection.HitArea = hitArea;
                        _connections.Add(newConnection);

                        connected = true;

                        // Add connection to xml
                        xmlService.UpdateCardInput(tgtId, 1, _wireSrcId);
                        break;
                    }

                    Canvas.Children.Remove(_tempWire);
                    connected = true;
                    break;
                }

                // --- For Input Port 2 ---
                if (Distance(dropX, dropY, in2.X, in2.Y) < snapRadius)
                {
                    if (!_connections.Any(c => c.TargetCardId == tgtId && c.TargetInputIndex == 2))
                    {
                        _tempWire.X2 = in2.X;
                        _tempWire.Y2 = in2.Y;
                        var newConnection = new Connection
                        {
                            SourceCardId = _wireSrcId,
                            TargetCardId = tgtId,
                            TargetInputIndex = 2,
                            LineShape = _tempWire
                        };

                        var hitArea = new ContentView
                        {
                            BackgroundColor = Colors.Transparent, // Use a bright color for testing if needed
                            InputTransparent = false
                        };
                        double margin = 10;
                        var minX = Math.Min(_tempWire.X1, _tempWire.X2) - margin;
                        var minY = Math.Min(_tempWire.Y1, _tempWire.Y2) - margin;
                        var width = Math.Abs(_tempWire.X2 - _tempWire.X1) + margin * 2;
                        var height = Math.Abs(_tempWire.Y2 - _tempWire.Y1) + margin * 2;
                        AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

                        var tapRecognizer = new TapGestureRecognizer();
                        tapRecognizer.Tapped += (s, e) =>
                        {
                            Debug.WriteLine("Connection hit area tapped!");
                            OnConnectionTapped(newConnection);
                        };
                        hitArea.GestureRecognizers.Add(tapRecognizer);
                        Canvas.Children.Add(hitArea);

                        newConnection.HitArea = hitArea;
                        _connections.Add(newConnection);

                        connected = true;

                        // Add connection to xml
                        xmlService.UpdateCardInput(tgtId, 2, _wireSrcId);
                        break;
                    }

                    Canvas.Children.Remove(_tempWire);
                    connected = true;
                    break;
                }
            }

            // 3) If we never connected, remove the temp wire
            if (!connected)
                Canvas.Children.Remove(_tempWire);

            // 4) Tear down the dot and overlay in either case
            Canvas.Children.Remove(_tempDot);
            Canvas.Children.Remove(_wireOverlay);

            _tempWire = null;
            _tempDot = null;
            _wireOverlay = null;
            _isDrawingWire = false;
        }
    }

    // ── Finish wiring on input tap ─────────────────────────────────────

    private static int GetCardId(IOutputProvider p)
    {
        return p switch
        {
            LogicGateCard lg => lg.Id,
            IOCard io => io.Id,
            _ => 0
        };
    }

    private void OnInTapped(object sender, int inputIndex)
    {
        // Console.WriteLine("OnInTapped called with inputIndex: " + inputIndex);

        // if (!_isDrawingWire || _tempWire == null)
        // {
        // 	System.Diagnostics.Debug.WriteLine("Not drawing wire or _tempWire is null.");
        // 	return;
        // }

        // var cv = (CardView)sender;
        // var tgt = _cardMap.First(kvp => kvp.Value == cv).Key;
        // var b = AbsoluteLayout.GetLayoutBounds(cv);

        // bool alreadyConnected = _connections.Any(c => c.TargetCardId == tgt && c.TargetInputIndex == inputIndex);
        // if (alreadyConnected)
        // {
        // 	System.Diagnostics.Debug.WriteLine("Input already connected.");
        // 	Canvas.Children.Remove(_wireOverlay);
        // 	_wireOverlay = null;
        // 	_isDrawingWire = false;
        // 	_tempDot = null;
        // 	_tempWire = null;
        // 	return;
        // }

        // // Set the final endpoint for the connection.
        // _tempWire.X2 = b.X;
        // _tempWire.Y2 = b.Y + b.Height * (inputIndex == 1 ? 0.25 : 0.75);

        // var newConnection = new Connection
        // {
        // 	SourceCardId = _wireSrcId,
        // 	TargetCardId = tgt,
        // 	TargetInputIndex = inputIndex,
        // 	LineShape = _tempWire
        // };

        // // Create a container for the hit area that will cover the connection.
        // var hitArea = new ContentView
        // {
        // 	// Temporarily set to Black so you can see it.
        // 	BackgroundColor = Colors.Black,
        // 	InputTransparent = false  // Ensure this view can receive taps.
        // };

        // // Calculate a bounding box for the hit area around the connection line.
        // double margin = 10; // extra margin around the line
        // double minX = Math.Min(_tempWire.X1, _tempWire.X2) - margin;
        // double minY = Math.Min(_tempWire.Y1, _tempWire.Y2) - margin;
        // double width = Math.Abs(_tempWire.X2 - _tempWire.X1) + margin * 2;
        // double height = Math.Abs(_tempWire.Y2 - _tempWire.Y1) + margin * 2;

        // // Output the calculated bounds for debugging.
        // System.Diagnostics.Debug.WriteLine($"HitArea bounds: minX: {minX}, minY: {minY}, width: {width}, height: {height}");
        // AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

        // // Attach a tap gesture recognizer to the hit area container.
        // var tapRecognizer = new TapGestureRecognizer();
        // tapRecognizer.Tapped += (s, e) =>
        // {
        // 	System.Diagnostics.Debug.WriteLine("Hit area tapped!");
        // 	OnConnectionTapped(newConnection);
        // };
        // hitArea.GestureRecognizers.Add(tapRecognizer);

        // // Add the connection line and the hit area container to the canvas.
        // Canvas.Children.Add(_tempWire);
        // Canvas.Children.Add(hitArea);

        // _connections.Add(newConnection);

        // // Remove the temporary overlay and clean up.
        // Canvas.Children.Remove(_wireOverlay);
        // _wireOverlay = null;
        // _isDrawingWire = false;
        // _tempDot = null;
        // _tempWire = null;
    }


    private async void OnConnectionTapped(Connection connection)
    {
        var line = connection.LineShape;

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

        deleteButton.Clicked += (s, e) =>
        {
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

        deleteButton.Clicked += (s, e) =>
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

    // ── Redraw permanent wires ─────────────────────────────────────────

    private void CreateConnection(int fromId, int toId, int inputIndex)
    {
        var line = new Line
        {
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };
        line.InputTransparent = true;
        Canvas.Children.Add(line);

        var conn = new Connection
        {
            SourceCardId = fromId,
            TargetCardId = toId,
            TargetInputIndex = inputIndex,
            LineShape = line
        };
        _connections.Add(conn);
        UpdateConnectionLine(conn);
    }

    private void UpdateConnectionLine(Connection c)
    {
        var s = _cardMap[c.SourceCardId];
        var d = _cardMap[c.TargetCardId];
        var sb = AbsoluteLayout.GetLayoutBounds(s);
        var db = AbsoluteLayout.GetLayoutBounds(d);

        c.LineShape.X1 = sb.X + sb.Width;
        c.LineShape.Y1 = sb.Y + sb.Height / 2;
        c.LineShape.X2 = db.X;
        c.LineShape.Y2 = db.Y + db.Height * (c.TargetInputIndex == 1 ? 0.25 : 0.75);
    }

    // ── PlaceCard helper ─────────────────────────────────────────────

    private void PlaceCard(int id, string type, double x, double y)
    {
        var vm = new CardViewModel(id, type);
        var cv = new CardView { BindingContext = vm };
        AbsoluteLayout.SetLayoutBounds(cv, new Rect(x, y, 120, 80));
        Canvas.Children.Add(cv);

        cv.PositionChanged += OnCardMoved;
        cv.OutputPortTapped += OnOutTapped;
        cv.InputPortTapped += OnInTapped;

        _cardMap[id] = cv;
    }

    // ── Resize the canvas ────────────────────────────────────────────

    private void UpdateCanvasSize()
    {
        const double pad = 20;
        if (_cardMap.Count == 0) return;

        var right = _cardMap.Values.Max(cv => AbsoluteLayout.GetLayoutBounds(cv).Right);
        var bottom = _cardMap.Values.Max(cv => AbsoluteLayout.GetLayoutBounds(cv).Bottom);

        Canvas.WidthRequest = Math.Max(right + pad, HorizontalScroll.Width);
        Canvas.HeightRequest = Math.Max(bottom + pad, VerticalScroll.Height);
    }

    private double Distance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
    }

    public void ReshuffleIds()
    {
        // 1. Gather all card info from the XML.
        var cards = new List<CardInfo>();

        var xmlService = new XmlStateService(statePath);

        var _doc = xmlService.Document;

        // Assume _doc is your XML document that holds the state.
        // Input cards (have no dependency)
        foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
        {
            var oldId = (int)elem.Attribute("id");
            cards.Add(new CardInfo { OldId = oldId, Type = "Input" });
        }

        // Logic gate cards (depend on two inputs possibly).
        foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
        {
            var oldId = (int)elem.Attribute("id");
            var info = new CardInfo { OldId = oldId, Type = "LogicGate" };

            // For each input attribute, add dependency if value is nonzero.
            var input1 = (int)elem.Attribute("input1");
            var input2 = (int)elem.Attribute("input2");
            if (input1 != 0)
                info.Dependencies.Add(input1);
            if (input2 != 0)
                info.Dependencies.Add(input2);

            cards.Add(info);
        }

        // Output cards (depend on one input).
        foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
        {
            var oldId = (int)elem.Attribute("id");
            var info = new CardInfo { OldId = oldId, Type = "Output" };
            var input1 = (int)elem.Attribute("input1");
            if (input1 != 0)
                info.Dependencies.Add(input1);
            cards.Add(info);
        }

        // 2. Build a dependency graph and perform a topological sort.
        // Create a lookup dictionary keyed on old ID.
        var lookup = cards.ToDictionary(c => c.OldId);

        // Compute in-degrees for each card.
        var inDegree = new Dictionary<int, int>();
        foreach (var card in cards) inDegree[card.OldId] = 0;
        foreach (var card in cards)
        foreach (var dep in card.Dependencies)
            // Increase in-degree for the card that depends on something.
            // (Here, each card's dependency is not about being depended upon; rather, the card
            // itself should have an in-degree corresponding to its number of dependencies.)
            inDegree[card.OldId]++;

        // Start with cards that have zero in-degree.
        var ready = new Queue<CardInfo>(cards.Where(c => inDegree[c.OldId] == 0));
        var sorted = new List<CardInfo>();

        while (ready.Count > 0)
        {
            var card = ready.Dequeue();
            sorted.Add(card);

            // For every card in the overall list that depends on this card,
            // decrement its in-degree.
            foreach (var dependent in cards.Where(c => c.Dependencies.Contains(card.OldId)))
            {
                inDegree[dependent.OldId]--;
                if (inDegree[dependent.OldId] == 0)
                    ready.Enqueue(dependent);
            }
        }

        // If there is a cycle, sorted.Count will not equal cards.Count
        if (sorted.Count != cards.Count)
            throw new Exception("A dependency cycle was detected among the cards. Reshuffling is not possible.");

        // 3. Assign new IDs in the sorted order.
        var newId = 1;
        var idMapping = new Dictionary<int, int>(); // mapping old -> new
        foreach (var card in sorted)
        {
            card.NewId = newId;
            idMapping[card.OldId] = newId;
            newId++;
        }

        // 4. Update the XML: 
        // Update input cards
        foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
        {
            var oldId = (int)elem.Attribute("id");
            if (idMapping.ContainsKey(oldId)) elem.SetAttributeValue("id", idMapping[oldId]);
        }

        // Update logic gate cards (update id, input1, input2)
        foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
        {
            var oldId = (int)elem.Attribute("id");
            if (idMapping.ContainsKey(oldId))
            {
                elem.SetAttributeValue("id", idMapping[oldId]);

                var input1 = (int)elem.Attribute("input1");
                var input2 = (int)elem.Attribute("input2");
                if (input1 != 0 && idMapping.ContainsKey(input1))
                    elem.SetAttributeValue("input1", idMapping[input1]);
                if (input2 != 0 && idMapping.ContainsKey(input2))
                    elem.SetAttributeValue("input2", idMapping[input2]);
            }
        }

        // Update output cards (update id and input1)
        foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
        {
            var oldId = (int)elem.Attribute("id");
            if (idMapping.ContainsKey(oldId))
            {
                elem.SetAttributeValue("id", idMapping[oldId]);
                var input1 = (int)elem.Attribute("input1");
                if (input1 != 0 && idMapping.ContainsKey(input1))
                    elem.SetAttributeValue("input1", idMapping[input1]);
            }
        }

        // Save the updated XML
        xmlService.Save();

        // 5. Update the in‑memory CardView objects on your canvas.
        // Here we update each CardView's BindingContext (a CardViewModel).
        foreach (var cardView in _cardMap.Values)
            if (cardView.BindingContext is CardViewModel vm)
                if (idMapping.ContainsKey(vm.Id))
                    vm.Id = idMapping[vm.Id];


        var newCardMap = new Dictionary<int, CardView>();
        foreach (var card in _cardMap.Values)
            if (card.BindingContext is CardViewModel vm)
                newCardMap[vm.Id] = card;

        _cardMap = newCardMap;
        
        foreach (var connection in _connections)
        {
            if (idMapping.ContainsKey(connection.SourceCardId))
                connection.SourceCardId = idMapping[connection.SourceCardId];
            
            if (idMapping.ContainsKey(connection.TargetCardId))
                connection.TargetCardId = idMapping[connection.TargetCardId];
        }

        xmlService.PrintStateFile();
    }


    private void loadInitialCanvas()
    {
        var xmlService = new XmlStateService(statePath);

        var _doc = xmlService.Document;

        foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
        {
            var cardID = (int)elem.Attribute("id");
            var gateType = "Input";
            var xPos = (double)elem.Attribute("xPos");
            var yPos = (double)elem.Attribute("yPos");

            var vm = new CardViewModel(cardID, gateType);

            vm.X = xPos;
            vm.Y = yPos;

            var cv = new CardView { BindingContext = vm };
            cv.DeleteRequested += Card_DeleteRequested;
            cv.PositionChanged += OnCardMoved;
            cv.OutputPortTapped += OnOutTapped;

            AbsoluteLayout.SetLayoutBounds(cv, new Rect(vm.X, vm.Y, 120, 80));
            Canvas.Children.Add(cv);

            _cardMap[cardID] = cv;
            UpdateCanvasSize();
        }

        foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
        {
            var cardID = (int)elem.Attribute("id");
            var gateType = "Output";
            var xPos = (double)elem.Attribute("xPos");
            var yPos = (double)elem.Attribute("yPos");
            var input1Id = (int)elem.Attribute("input1");

            var vm = new CardViewModel(cardID, gateType);

            vm.X = xPos;
            vm.Y = yPos;

            var cv = new CardView { BindingContext = vm };
            cv.DeleteRequested += Card_DeleteRequested;
            cv.PositionChanged += OnCardMoved;
            cv.InputPortTapped += OnInTapped;

            AbsoluteLayout.SetLayoutBounds(cv, new Rect(vm.X, vm.Y, 120, 80));
            Canvas.Children.Add(cv);

            _cardMap[cardID] = cv;
            UpdateCanvasSize();
        }

        foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
        {
            var cardID = (int)elem.Attribute("id");
            var gateType = (string)elem.Attribute("gateType");
            var xPos = (double)elem.Attribute("xPos");
            var yPos = (double)elem.Attribute("yPos");
            var input1Id = (int)elem.Attribute("input1");
            var input2Id = (int)elem.Attribute("input2");

            var vm = new CardViewModel(cardID, gateType);

            vm.X = xPos;
            vm.Y = yPos;

            var cv = new CardView { BindingContext = vm };

            cv.DeleteRequested += Card_DeleteRequested;
            cv.PositionChanged += OnCardMoved;
            cv.InputPortTapped += OnInTapped;
            cv.OutputPortTapped += OnOutTapped;

            AbsoluteLayout.SetLayoutBounds(cv, new Rect(vm.X, vm.Y, 120, 80));
            Canvas.Children.Add(cv);

            _cardMap[cardID] = cv;
            UpdateCanvasSize();
        }

        // Rebuild connections
        foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
        {
            var cardId = (int)elem.Attribute("id");
            var sourceId = (int)elem.Attribute("input1"); // For outputs, input1 is the connected source

            // get the cardviews

            var targetCv = _cardMap[cardId];
            var sourceCv = _cardMap[sourceId];

            var targetBounds = AbsoluteLayout.GetLayoutBounds(targetCv);
            var sourceBounds = AbsoluteLayout.GetLayoutBounds(sourceCv);

            var wireStartX = sourceBounds.X + sourceBounds.Width;
            var wireStartY = sourceBounds.Y + sourceBounds.Height / 2;

            var in1Point = new Point(targetBounds.X, targetBounds.Y + targetBounds.Height * 0.25);

            var connectionWire = new Line
            {
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                X1 = wireStartX, Y1 = wireStartY,
                X2 = in1Point.X, Y2 = in1Point.Y
            };

            if (sourceId != 0)
            {
                var newConnection = new Connection
                {
                    SourceCardId = sourceId,
                    TargetCardId = cardId,
                    TargetInputIndex = 1,
                    LineShape = connectionWire
                };

                var hitArea = new ContentView
                {
                    BackgroundColor = Colors.Transparent,
                    InputTransparent = false
                };

                // Calculate the bounding box for the hit area with extra margin for easier tapping.
                double margin = 10;
                var minX = Math.Min(connectionWire.X1, connectionWire.X2) - margin;
                var minY = Math.Min(connectionWire.Y1, connectionWire.Y2) - margin;
                var width = Math.Abs(connectionWire.X2 - connectionWire.X1) + margin * 2;
                var height = Math.Abs(connectionWire.Y2 - connectionWire.Y1) + margin * 2;
                AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

                // Attach a tap gesture recognizer for bringing up the delete button.
                var tapRecognizer = new TapGestureRecognizer();
                tapRecognizer.Tapped += (s, e) =>
                {
                    Debug.WriteLine("Connection hit area tapped!");
                    OnConnectionTapped(newConnection);
                };
                hitArea.GestureRecognizers.Add(tapRecognizer);

                connectionWire.InputTransparent = true; 
                Canvas.Children.Add(connectionWire);

                Canvas.Children.Add(hitArea);

                newConnection.HitArea = hitArea;
                _connections.Add(newConnection);
            }
        }

        // Rebuild connections for LogicGateCards (can have input1 and/or input2)
        foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
        {
            var cardId = (int)elem.Attribute("id");
            var input1SourceId = (int)elem.Attribute("input1");
            var input2SourceId = (int)elem.Attribute("input2");

            // Get the target card view from the map.
            var targetCv = _cardMap[cardId];
            var targetBounds = AbsoluteLayout.GetLayoutBounds(targetCv);

            // Process connection for input1, if connected.
            if (input1SourceId != 0)
            {
                // Get source card view from the map.
                var sourceCv = _cardMap[input1SourceId];
                var sourceBounds = AbsoluteLayout.GetLayoutBounds(sourceCv);

                // Setup the starting point at the right side of the source card.
                var wireStartX = sourceBounds.X + sourceBounds.Width;
                var wireStartY = sourceBounds.Y + sourceBounds.Height / 2;

                // For input1, assume the target connection point is at 25% down from the top.
                var in1Point = new Point(targetBounds.X, targetBounds.Y + targetBounds.Height * 0.25);

                // Create the connection line.
                var connectionWire = new Line
                {
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    X1 = wireStartX, Y1 = wireStartY,
                    X2 = in1Point.X, Y2 = in1Point.Y
                };

                // Create the connection object.
                var newConnection = new Connection
                {
                    SourceCardId = input1SourceId,
                    TargetCardId = cardId,
                    TargetInputIndex = 1,
                    LineShape = connectionWire
                };

                // Build a transparent hit area around the connection line.
                var hitArea = new ContentView
                {
                    BackgroundColor = Colors.Transparent,
                    InputTransparent = false
                };

                double margin = 10;
                var minX = Math.Min(connectionWire.X1, connectionWire.X2) - margin;
                var minY = Math.Min(connectionWire.Y1, connectionWire.Y2) - margin;
                var width = Math.Abs(connectionWire.X2 - connectionWire.X1) + margin * 2;
                var height = Math.Abs(connectionWire.Y2 - connectionWire.Y1) + margin * 2;
                AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

                // Add a tap recognizer to let the user delete the connection.
                var tapRecognizer = new TapGestureRecognizer();
                tapRecognizer.Tapped += (s, e) =>
                {
                    Debug.WriteLine("Connection hit area tapped!");
                    OnConnectionTapped(newConnection);
                };
                hitArea.GestureRecognizers.Add(tapRecognizer);

                // Add the visuals to the canvas.
                connectionWire.InputTransparent = true; 
                Canvas.Children.Add(connectionWire);
                Canvas.Children.Add(hitArea);

                // Save hit area reference and track the connection.
                newConnection.HitArea = hitArea;
                _connections.Add(newConnection);
            }

            // Process connection for input2, if connected.
            if (input2SourceId != 0)
            {
                // Get the source card view.
                var sourceCv = _cardMap[input2SourceId];
                var sourceBounds = AbsoluteLayout.GetLayoutBounds(sourceCv);

                var wireStartX = sourceBounds.X + sourceBounds.Width;
                var wireStartY = sourceBounds.Y + sourceBounds.Height / 2;

                // For input2, assume the target connection point is at 75% down from the top.
                var in2Point = new Point(targetBounds.X, targetBounds.Y + targetBounds.Height * 0.75);

                var connectionWire = new Line
                {
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    X1 = wireStartX, Y1 = wireStartY,
                    X2 = in2Point.X, Y2 = in2Point.Y
                };

                var newConnection = new Connection
                {
                    SourceCardId = input2SourceId,
                    TargetCardId = cardId,
                    TargetInputIndex = 2,
                    LineShape = connectionWire
                };

                var hitArea = new ContentView
                {
                    BackgroundColor = Colors.Transparent,
                    InputTransparent = false
                };

                double margin = 10;
                var minX = Math.Min(connectionWire.X1, connectionWire.X2) - margin;
                var minY = Math.Min(connectionWire.Y1, connectionWire.Y2) - margin;
                var width = Math.Abs(connectionWire.X2 - connectionWire.X1) + margin * 2;
                var height = Math.Abs(connectionWire.Y2 - connectionWire.Y1) + margin * 2;
                AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

                var tapRecognizer = new TapGestureRecognizer();
                tapRecognizer.Tapped += (s, e) =>
                {
                    Debug.WriteLine("Connection hit area tapped!");
                    OnConnectionTapped(newConnection);
                };
                hitArea.GestureRecognizers.Add(tapRecognizer);

                connectionWire.InputTransparent = true;
                Canvas.Children.Add(connectionWire);
                Canvas.Children.Add(hitArea);

                newConnection.HitArea = hitArea;
                _connections.Add(newConnection);
            }
        }
    }

    // Helper class to store card info for topological sorting.
    private class CardInfo
    {
        public int OldId { get; set; }
        public int NewId { get; set; }
        public string Type { get; set; } // "Input", "LogicGate", "Output"
        public List<int> Dependencies { get; } = new(); // older IDs of cards this one depends on.
    }
}