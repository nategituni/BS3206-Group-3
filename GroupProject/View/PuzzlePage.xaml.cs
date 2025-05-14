using System.Xml.Linq;
using GroupProject.Model.LogicModel;
using GroupProject.Services;
using GroupProject.ViewModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

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

	private readonly string statePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");

    private Dictionary<int, CardView> _cardMap = new();
    private readonly List<Connection> _connections = new();
    private double _dotStartX, _dotStartY;

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

    private async void Load_Clicked(object s, EventArgs e)
    {
        var pick = await FilePicker.PickAsync(new PickOptions { PickerTitle = "Select puzzle XML" });
        if (pick == null) return;

        var xml = await File.ReadAllTextAsync(pick.FullPath);
        _stateParser = new StateParser(XDocument.Parse(xml));
        _stateFilePath = pick.FullPath;
        var (inputs, gates, outputs) = _stateParser.parseCards();

        Canvas.Children.Clear();
        _cardMap.Clear();
        _connections.Clear();
        _nextId = 1;

        const double h = 200, v = 120, m = 20;
        for (var i = 0; i < inputs.Count; i++)
        {
            PlaceCard(inputs[i].Id, "IN", m, m + i * v);
            _nextId = Math.Max(_nextId, inputs[i].Id + 1);
        }

        // TODO: place gates & outputs...
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
		{
			if (child is CardView cardView && cardView.BindingContext is CardViewModel viewModel)
			{
				// Check if this is an output card
				if (viewModel.GateType.Equals("Output", StringComparison.OrdinalIgnoreCase))
				{
					// Find the matching outputCard from the list using the id
					var matchingOutput = outputCards.FirstOrDefault(o => o.Id == viewModel.Id);
					if (matchingOutput != null)
					{
						// Update the OutputValueLabel text to "1" if true, or "0" if false
						cardView.OutputValueLabel.Text = matchingOutput.Output ? "1" : "0";
					}
				}
			}
		}
	}

    // ── AddGate helper ─────────────────────────────────────────────────

	private void AddGate(string gateType)
	{
		var xmlService = new XmlStateService(statePath);
		
		// Fetch all existing IDs
		var existingIds = xmlService.GetAllIds();
		
		// Find the next available ID
		int id = _nextId;
		while (existingIds.Contains(id))
		{
			id++;
		}
		_nextId = id + 1; // Increment for next addition

		var vm = new CardViewModel(id, gateType);
		Point spawnStart = new Point(50, 50);
		Point candidate = GetNearestFreeSpot(spawnStart, new Size(120, 80));

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
		
		int cardId = vm.Id;
		// Remove the card from the canvas
		Canvas.Children.Remove(cardView);
		// Remove it from your map
		if (_cardMap.ContainsKey(cardId))
			_cardMap.Remove(cardId);
		
		// Also remove any connections that reference this card
		var connectionsToRemove = _connections
			.Where(c => c.SourceCardId == cardId || c.TargetCardId == cardId)
			.ToList();
		foreach(var conn in connectionsToRemove)
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
		double step = 10;    // increment in pixels
		double bestDistance = double.MaxValue;
		Point bestCandidate = start;

		// Search in a square around the spawn start.
		// Adjust the range if necessary (here, -300 to +300 pixels in both dimensions)
		for (int offsetX = -300; offsetX <= 300; offsetX += (int)step)
		{
			for (int offsetY = -300; offsetY <= 300; offsetY += (int)step)
			{
				Point candidate = new Point(start.X + offsetX, start.Y + offsetY);
				
				if (candidate.X < 10 || candidate.Y < 10)
					continue;

				Rect candidateRect = new Rect(candidate, gateSize);
				if (!IsOverlapping(candidateRect))
				{
					double distance = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
					if (distance < bestDistance)
					{
						bestDistance = distance;
						bestCandidate = candidate;
					}
				}
			}
		}
		return bestCandidate;
	}

	private bool IsOverlapping(Rect candidateRect)
	{
		foreach (var card in _cardMap.Values)
		{
			Rect existingRect = AbsoluteLayout.GetLayoutBounds(card);
			Rect expandedRect = new Rect(existingRect.X - 10, existingRect.Y -10, existingRect.Width + 20, existingRect.Height + 20);
			if (candidateRect.IntersectsWith(expandedRect))
				return true;
		}
		return false;
	}

    // ── Card drag ──────────────────────────────────────────────────────

	private CancellationTokenSource _dragEndCts;

	private void OnCardMoved(object s, PositionChangedEventArgs e)
	{
		if (_isDrawingWire) return;

		var cv = (CardView)s;
		var id = _cardMap.First(kvp => kvp.Value == cv).Key;

		foreach (var c in _connections.Where(c => c.SourceCardId == id || c.TargetCardId == id))
			UpdateConnectionLine(c);

		var bounds = AbsoluteLayout.GetLayoutBounds(cv);
		double newX = bounds.X;
		double newY = bounds.Y;

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
        _wireOverlay = new BoxView { BackgroundColor = Colors.Transparent };
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

						// Add the connection to your collection.
						_connections.Add(newConnection);

						// Now create and add the invisible hit area.
						var hitArea = new ContentView
						{
							BackgroundColor = Colors.Transparent, // set to a visible color (e.g. Colors.Black) for debugging
							InputTransparent = false  // ensure it can receive touch input
						};

						// Calculate the bounding box for the hit area with extra margin for easier tapping.
						double margin = 10;
						double minX = Math.Min(_tempWire.X1, _tempWire.X2) - margin;
						double minY = Math.Min(_tempWire.Y1, _tempWire.Y2) - margin;
						double width = Math.Abs(_tempWire.X2 - _tempWire.X1) + margin * 2;
						double height = Math.Abs(_tempWire.Y2 - _tempWire.Y1) + margin * 2;
						AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

						// Attach a tap gesture recognizer for bringing up the delete button.
						var tapRecognizer = new TapGestureRecognizer();
						tapRecognizer.Tapped += (s, e) =>
						{
							System.Diagnostics.Debug.WriteLine("Connection hit area tapped!");
							OnConnectionTapped(newConnection);
						};
						hitArea.GestureRecognizers.Add(tapRecognizer);

						// Add the hit area (and the connection line already exists) to the Canvas.
						Canvas.Children.Add(hitArea);

						connected = true;

						// Add connection to xml
						xmlService.UpdateCardInput(tgtId, 1, _wireSrcId);
						break;
					}
					else
					{
						Canvas.Children.Remove(_tempWire);
						connected = true;
						break;
					}
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
						_connections.Add(newConnection);

						var hitArea = new ContentView
						{
							BackgroundColor = Colors.Transparent,  // Use a bright color for testing if needed
							InputTransparent = false
						};
						double margin = 10;
						double minX = Math.Min(_tempWire.X1, _tempWire.X2) - margin;
						double minY = Math.Min(_tempWire.Y1, _tempWire.Y2) - margin;
						double width = Math.Abs(_tempWire.X2 - _tempWire.X1) + margin * 2;
						double height = Math.Abs(_tempWire.Y2 - _tempWire.Y1) + margin * 2;
						AbsoluteLayout.SetLayoutBounds(hitArea, new Rect(minX, minY, width, height));

						var tapRecognizer = new TapGestureRecognizer();
						tapRecognizer.Tapped += (s, e) =>
						{
							System.Diagnostics.Debug.WriteLine("Connection hit area tapped!");
							OnConnectionTapped(newConnection);
						};
						hitArea.GestureRecognizers.Add(tapRecognizer);
						Canvas.Children.Add(hitArea);

						connected = true;

						// Add connection to xml
						xmlService.UpdateCardInput(tgtId, 2, _wireSrcId);
						break;
					}
					else
					{
						Canvas.Children.Remove(_tempWire);
						connected = true;
						break;
					}
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
		double centerX = (line.X1 + line.X2) / 2;
		double centerY = (line.Y1 + line.Y2) / 2;
		
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
			CornerRadius = 15,   // Circular button.
		};

		deleteButton.Clicked += (s, e) =>
		{
			// Remove connection's visual elements.
			if (Canvas.Children.Contains(connection.LineShape))
				Canvas.Children.Remove(connection.LineShape);

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
			if (Canvas.Children.Contains(deleteButton))
				Canvas.Children.Remove(deleteButton);
			_connections.Remove(connection);
		};

		// Wait for 1 second, then auto-remove the button if it hasn't been interacted with.
		await Task.Delay(1000);
		if (Canvas.Children.Contains(deleteButton))
		{
			Canvas.Children.Remove(deleteButton);
		}
	}

    // ── Redraw permanent wires ─────────────────────────────────────────

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

	// Helper class to store card info for topological sorting.
	class CardInfo
	{
		public int OldId { get; set; }
		public int NewId { get; set; } = 0;
		public string Type { get; set; } // "Input", "LogicGate", "Output"
		public List<int> Dependencies { get; set; } = new List<int>(); // older IDs of cards this one depends on.
	}

	public void ReshuffleIds()
	{
		// 1. Gather all card info from the XML.
		List<CardInfo> cards = new List<CardInfo>();

		var xmlService = new XmlStateService(statePath);

		XDocument _doc = xmlService.Document;

		// Assume _doc is your XML document that holds the state.
		// Input cards (have no dependency)
		foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
		{
			int oldId = (int)elem.Attribute("id");
			cards.Add(new CardInfo { OldId = oldId, Type = "Input" });
		}

		// Logic gate cards (depend on two inputs possibly).
		foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
		{
			int oldId = (int)elem.Attribute("id");
			var info = new CardInfo { OldId = oldId, Type = "LogicGate" };

			// For each input attribute, add dependency if value is nonzero.
			int input1 = (int)elem.Attribute("input1");
			int input2 = (int)elem.Attribute("input2");
			if (input1 != 0)
				info.Dependencies.Add(input1);
			if (input2 != 0)
				info.Dependencies.Add(input2);

			cards.Add(info);
		}

		// Output cards (depend on one input).
		foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
		{
			int oldId = (int)elem.Attribute("id");
			var info = new CardInfo { OldId = oldId, Type = "Output" };
			int input1 = (int)elem.Attribute("input1");
			if (input1 != 0)
				info.Dependencies.Add(input1);
			cards.Add(info);
		}

		// 2. Build a dependency graph and perform a topological sort.
		// Create a lookup dictionary keyed on old ID.
		Dictionary<int, CardInfo> lookup = cards.ToDictionary(c => c.OldId);

		// Compute in-degrees for each card.
		Dictionary<int, int> inDegree = new Dictionary<int, int>();
		foreach (var card in cards)
		{
			inDegree[card.OldId] = 0;
		}
		foreach (var card in cards)
		{
			foreach (var dep in card.Dependencies)
			{
				// Increase in-degree for the card that depends on something.
				// (Here, each card's dependency is not about being depended upon; rather, the card
				// itself should have an in-degree corresponding to its number of dependencies.)
				inDegree[card.OldId]++;
			}
		}

		// Start with cards that have zero in-degree.
		Queue<CardInfo> ready = new Queue<CardInfo>(cards.Where(c => inDegree[c.OldId] == 0));
		List<CardInfo> sorted = new List<CardInfo>();

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
		{
			throw new Exception("A dependency cycle was detected among the cards. Reshuffling is not possible.");
		}

		// 3. Assign new IDs in the sorted order.
		int newId = 1;
		Dictionary<int, int> idMapping = new Dictionary<int, int>(); // mapping old -> new
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
			int oldId = (int)elem.Attribute("id");
			if (idMapping.ContainsKey(oldId))
			{
				elem.SetAttributeValue("id", idMapping[oldId]);
			}
		}
		// Update logic gate cards (update id, input1, input2)
		foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
		{
			int oldId = (int)elem.Attribute("id");
			if (idMapping.ContainsKey(oldId))
			{
				elem.SetAttributeValue("id", idMapping[oldId]);

				int input1 = (int)elem.Attribute("input1");
				int input2 = (int)elem.Attribute("input2");
				if (input1 != 0 && idMapping.ContainsKey(input1))
					elem.SetAttributeValue("input1", idMapping[input1]);
				if (input2 != 0 && idMapping.ContainsKey(input2))
					elem.SetAttributeValue("input2", idMapping[input2]);
			}
		}
		// Update output cards (update id and input1)
		foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
		{
			int oldId = (int)elem.Attribute("id");
			if (idMapping.ContainsKey(oldId))
			{
				elem.SetAttributeValue("id", idMapping[oldId]);
				int input1 = (int)elem.Attribute("input1");
				if (input1 != 0 && idMapping.ContainsKey(input1))
					elem.SetAttributeValue("input1", idMapping[input1]);
			}
		}
		// Save the updated XML
		xmlService.Save();

		// 5. Update the in‑memory CardView objects on your canvas.
		// Here we update each CardView's BindingContext (a CardViewModel).
		foreach (var cardView in _cardMap.Values)
		{
			if (cardView.BindingContext is CardViewModel vm)
			{
				if (idMapping.ContainsKey(vm.Id))
				{
					vm.Id = idMapping[vm.Id];
				}
			}
		}


		var newCardMap = new Dictionary<int, CardView>();
		foreach (var card in _cardMap.Values)
		{
			if (card.BindingContext is CardViewModel vm)
			{
				newCardMap[vm.Id] = card;
			}
		}
		_cardMap = newCardMap;


		xmlService.PrintStateFile();
	}


	private void loadInitialCanvas()
	{
		var xmlService = new XmlStateService(statePath);

		XDocument _doc = xmlService.Document;

		foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
		{
			int cardID = (int)elem.Attribute("id");
			string gateType = "Input";
			double xPos = (double)elem.Attribute("xPos");
			double yPos = (double)elem.Attribute("yPos");

			var vm = new CardViewModel(cardID, gateType);

			vm.X = xPos;
			vm.Y = yPos;

			var cv = new CardView { BindingContext = vm };
			cv.DeleteRequested += Card_DeleteRequested;
			cv.PositionChanged += OnCardMoved;
			cv.OutputPortTapped += OnOutTapped;

			AbsoluteLayout.SetLayoutBounds(cv, new Rect(vm.X, vm.Y, 120, 80));
			Canvas.Children.Add(cv);
			UpdateCanvasSize();
		}

		foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
		{
			int cardID = (int)elem.Attribute("id");
			string gateType = "Output";
			double xPos = (double)elem.Attribute("xPos");
			double yPos = (double)elem.Attribute("yPos");

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
			int cardID = (int)elem.Attribute("id");
			string gateType = (string)elem.Attribute("gateType");
			double xPos = (double)elem.Attribute("xPos");
			double yPos = (double)elem.Attribute("yPos");

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
	}
}