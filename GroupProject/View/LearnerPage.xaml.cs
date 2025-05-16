using System.Xml.Linq;
using GroupProject.Model.LogicModel;
using GroupProject.Model.LearnerModel;
using GroupProject.Services;
using Path = System.IO.Path;
using GroupProject.ViewModel;
using Microsoft.Maui.Controls.Shapes;
using GroupProject.Model.Utilities;

namespace GroupProject.View;

public partial class LearnerPage : ContentPage
{
	private readonly List<Connection> _connections = new();

	private readonly string statePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");

	private readonly Dictionary<long, HashSet<int>> _occupiedAreas = new();

	private Dictionary<int, CardView> _cardMap = new();

	private bool _isDrawingWire;

	private CancellationTokenSource _dragEndCts;

	private int numberOfInputs;

	private int numberOfOutputs;


	public LearnerPage()
	{
		InitializeComponent();
		BindingContext = new LearnerViewModel();
		InitializeSidebar();
	}

	public void InitializeSidebar()
	{
		var selectionLayout = new StackLayout { Orientation = StackOrientation.Vertical }; 

		var inputLabel = new Label { Text = "Number of Inputs:" };
		var inputEntry = new Entry
		{
			Placeholder = "e.g., 2",
			Keyboard = Keyboard.Numeric,
			WidthRequest = 50
		};
		inputEntry.AutomationId = "TruthTableInputEntryField";

		var outputLabel = new Label { Text = "Number of Outputs:" };
		var outputEntry = new Entry
		{
			Placeholder = "e.g., 1",
			Keyboard = Keyboard.Numeric,
			WidthRequest = 50
		};
		outputEntry.AutomationId = "TruthTableOutputEntryField";

		var generateButton = new Button { Text = "Generate Truth Table" };
		generateButton.AutomationId = "GenerateTruthTableBtn";


		generateButton.Clicked += (s, e) =>
		{
			numberOfInputs = int.Parse(inputEntry.Text);
			numberOfOutputs = int.Parse(outputEntry.Text);
			GenerateTruthTable(numberOfInputs, numberOfOutputs);
		};

		selectionLayout.Children.Add(inputLabel);
		selectionLayout.Children.Add(inputEntry);
		selectionLayout.Children.Add(outputLabel);
		selectionLayout.Children.Add(outputEntry);
		selectionLayout.Children.Add(generateButton);

		Sidebar.Children.Clear();
		Sidebar.Children.Add(selectionLayout);
	}

	void GenerateTruthTable(int numberOfInputs, int numberOfOutputs)
	{
		Sidebar.Children.Clear();

		var scrollView = new ScrollView { Orientation = ScrollOrientation.Both };
		var truthTableLayout = new StackLayout { Orientation = StackOrientation.Vertical };

		var headerLayout = new StackLayout { Orientation = StackOrientation.Horizontal };

		// Create headers for inputs
		for (int i = 0; i < numberOfInputs; i++)
		{
			var inputHeader = new Label
			{
				Text = $"In {i + 1}",
				FontAttributes = FontAttributes.Bold,
				WidthRequest = 50,
				HorizontalTextAlignment = TextAlignment.Center
			};
			// Set a unique Automation ID for each input header
			inputHeader.AutomationId = $"InputHeader_{i + 1}";

			headerLayout.Children.Add(inputHeader);
		}

		// Create headers for outputs
		for (int i = 0; i < numberOfOutputs; i++)
		{
			var outputHeader = new Label
			{
				Text = $"Out {i + 1}",
				FontAttributes = FontAttributes.Bold,
				WidthRequest = 50,
				HorizontalTextAlignment = TextAlignment.Center
			};
			// Set a unique Automation ID for each output header
			outputHeader.AutomationId = $"OutputHeader_{i + 1}";

			headerLayout.Children.Add(outputHeader);
		}

		truthTableLayout.Children.Add(headerLayout); 

		int numberOfRows = (int)Math.Pow(2, numberOfInputs);
		for (int row = 0; row < numberOfRows; row++)
		{
			var rowLayout = new StackLayout { Orientation = StackOrientation.Horizontal };

			// Generate input entries for this row
			for (int col = 0; col < numberOfInputs; col++)
			{
				var inputEntry = new Entry
				{
					Placeholder = "0 or 1",
					Keyboard = Keyboard.Numeric,
					WidthRequest = 50
				};
				// Example AutomationID that includes row and column info
				inputEntry.AutomationId = $"InputEntry_Row{row}_Col{col}";

				rowLayout.Children.Add(inputEntry);
			}

			// Generate output entries for this row
			for (int col = 0; col < numberOfOutputs; col++)
			{
				var outputEntry = new Entry
				{
					Placeholder = "0 or 1",
					Keyboard = Keyboard.Numeric,
					WidthRequest = 50
				};
				// Example AutomationID that includes row and column info
				outputEntry.AutomationId = $"OutputEntry_Row{row}_Col{col}";

				rowLayout.Children.Add(outputEntry);
			}

			truthTableLayout.Children.Add(rowLayout);
		}

		scrollView.Content = truthTableLayout;
		Sidebar.Children.Add(scrollView);
	}


	private async void Save_Clicked(object sender, EventArgs e)
	{

		string userEmail = Preferences.Get("UserEmail", null);

		if (string.IsNullOrEmpty(userEmail))
		{
			await DisplayAlert("Error", "User email not found. Please log in.", "OK");
			return;
		}

		int userId = await AuthService.GetUserIdByEmailAsync(userEmail);

		string userInput = await DisplayPromptAsync("Save Puzzle", "Enter puzzle name:", "OK", "Cancel", "Puzzle Name");

		await PuzzleService.SavePuzzleAsync(userId, userInput);
	}

	private void Clear_Clicked(object s, EventArgs e)
	{
		var xmlService = new XmlStateService(statePath);

		xmlService.ClearStateFile();

		Canvas.Children.Clear();

		if (_cardMap != null)
		{
			_cardMap.Clear();
		}

		if (_connections != null)
		{
			_connections.Clear();
		}

		
		Sidebar.Children.Clear();
		InitializeSidebar();

		DisplayAlert("State Cleared", "All cards and connections have been removed.", "OK");
	}

	private async void Learn_Clicked(object s, EventArgs e)
	{
		LoadingOverlay.IsVisible = true;

		Canvas.Children.Clear();

		try
		{
			await Task.Run(Learner);
		}
		finally
		{
			LoadingOverlay.IsVisible = false;
		}
	}

	private void Learner()
	{
		if (BindingContext is not LearnerViewModel lvm) return;
		LogicState BuildInitialState(int numberOfInputs, int numberOfOutputs)
		{
			LogicState state = new LogicState();

			for (int i = 0; i < numberOfInputs; i++)
			{
				state.Gates.Add($"Input {i + 1}");
			}

			for (int i = 0; i < numberOfOutputs; i++)
			{
				state.Gates.Add($"Output {i + 1}");
			}

			return state;
		}

		bool DoesStateSolvePuzzle(LogicState state)
		{
			var truthRows = GetTruthTableRowsFromSidebar();
			var xmlService = new XmlStateService(statePath);
			bool overallValid = true;

			lvm.SaveStateToXml(state, lvm.GetXmlStateService());

			foreach (var row in truthRows)
			{
				int inputIndex = 0;
				foreach (var gate in state.Gates)
				{
					if (gate.StartsWith("Input"))
					{
						xmlService.UpdateInputCardValue(inputIndex + 1, row.Inputs[inputIndex]);
						inputIndex++;
					}
				}

				try
				{
					lvm.ReshuffleIds();  
				}
				catch (Exception ex)
				{
					if (ex.Message.Contains("A dependency cycle was detected"))
					{
						return false;  
					}
					else
					{
						throw; 
					}
				}

				var calculateParser = new StateParser();

				var (_, _, outputCardsCalculated) = calculateParser.parseCards();

				for (int i = 0; i < numberOfOutputs; i++)
				{
					if (outputCardsCalculated[i].Output != row.ExpectedOutputs[i])
					{
						overallValid = false;
						break;
					}
				}

				if (!overallValid)
				{
					break;
				}
			}
			return overallValid;
		}

		void SearchForValidState()
		{
			
			LogicState initialState = BuildInitialState(numberOfInputs, numberOfOutputs);

			Queue<LogicState> stateQueue = new Queue<LogicState>();
			HashSet<int> visitedStates = new HashSet<int>();  

			stateQueue.Enqueue(initialState);

			while (stateQueue.Count > 0)
			{
				LogicState currentState = stateQueue.Dequeue();

				lvm.SaveStateToXml(currentState, lvm.GetXmlStateService());	

				if (DoesStateSolvePuzzle(currentState))
				{
					Dispatcher.Dispatch(() =>
					{
						Canvas.Children.Clear();
						_cardMap.Clear();
						_connections.Clear();
						AssignCardPositions();
						loadCanvasFromXml();
						DisplayAlert("Success", "Found a valid state!", "OK");
					});
					return;
				}

				foreach (LogicState neighbor in GenerateNeighborStates(currentState))
				{
					int stateHash = neighbor.GetHashCode();
					if (!visitedStates.Contains(stateHash))
					{
						visitedStates.Add(stateHash);
						stateQueue.Enqueue(neighbor);
					}
				}
			}
			DisplayAlert("Failure", "No valid state found.", "OK");
		}

		IEnumerable<LogicState> GenerateNeighborStates(LogicState currentState)
		{
			List<LogicState> neighbors = new List<LogicState>();

			foreach (var gateType in new[] { "And", "Or", "Xor", "Not", "Nand", "Nor", "Xnor" })  
			{
				LogicState newState = currentState.Clone();
				newState.Gates.Add(gateType);
				neighbors.Add(newState);
			}

			for (int sourceIndex = 0; sourceIndex < currentState.Gates.Count; sourceIndex++)
			{
				for (int targetIndex = 0; targetIndex < currentState.Gates.Count; targetIndex++)
				{
					if (sourceIndex == targetIndex)  
						continue;

					string targetGate = currentState.Gates[targetIndex];

					if (targetGate.StartsWith("Output") && currentState.Connections.Count(c => c.to == targetIndex) >= 1)
						continue;

					if (!targetGate.StartsWith("Input") && !targetGate.StartsWith("Output") && currentState.Connections.Count(c => c.to == targetIndex) >= 2)
						continue;

					LogicState neighbor = currentState.Clone();
					neighbor.Connections.Add((sourceIndex, targetIndex));
					neighbors.Add(neighbor);
				}
			}

			return neighbors;
		}
		
		SearchForValidState();
	}

	private void AssignCardPositions()
	{
		var xmlService = new XmlStateService(statePath);
		var _doc = xmlService.Document;

		Size cardSize = new Size(140, 100);
		Point startPoint = new Point(50, 50);
		double step = 10;

		List<Rect> occupiedRects = new List<Rect>();

		bool IsOverlapping(Rect candidate)
		{
			return occupiedRects.Any(r => r.IntersectsWith(candidate));
		}

		Point GetNearestFreeSpot(Point start, Size size)
		{
			double searchRadius = step;
			double bestDistance = double.MaxValue;
			Point bestCandidate = start;

			
			while (true)
			{
				for (double offsetX = 0; offsetX <= searchRadius; offsetX += step)
				{
					for (double offsetY = 0; offsetY <= searchRadius; offsetY += step)
					{
						Point candidate = new Point(start.X + offsetX, start.Y + offsetY);
						
						if (candidate.X < 10 || candidate.Y < 10)
							continue;
						Rect candidateRect = new Rect(candidate, size);
						if (!IsOverlapping(candidateRect))
						{
							double distance = Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
							if (distance < bestDistance)
							{
								bestDistance = distance;
								bestCandidate = candidate;
								
								return bestCandidate;
							}
						}
					}
				}
				searchRadius += step;
			}
		}
		
		foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
		{
			int id = (int)elem.Attribute("id");
			Point pos = GetNearestFreeSpot(startPoint, cardSize);
			occupiedRects.Add(new Rect(pos, cardSize));
			xmlService.SetCardPosition(id, pos.X, pos.Y);
		}

		foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
		{
			int id = (int)elem.Attribute("id");
			Point pos = GetNearestFreeSpot(startPoint, cardSize);
			occupiedRects.Add(new Rect(pos, cardSize));
			xmlService.SetCardPosition(id, pos.X, pos.Y);
		}

		foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
		{
			int id = (int)elem.Attribute("id");
			Point pos = GetNearestFreeSpot(startPoint, cardSize);
			occupiedRects.Add(new Rect(pos, cardSize));
			xmlService.SetCardPosition(id, pos.X, pos.Y);
		}
	}

	private List<TruthTableRow> GetTruthTableRowsFromSidebar()
	{
		var truthRows = new List<TruthTableRow>();

		var scrollView = Sidebar.Children.OfType<ScrollView>().FirstOrDefault();
		if (scrollView?.Content is not StackLayout truthTableLayout)
			return truthRows;

		
		var dataRows = truthTableLayout.Children.OfType<StackLayout>().Skip(1).ToList();

		foreach (var rowLayout in dataRows)
		{
			var entries = rowLayout.Children.OfType<Entry>().ToList();
			if (entries.Count < numberOfInputs + numberOfOutputs)
				continue; 

			bool[] inputs = new bool[numberOfInputs];
			bool[] outputs = new bool[numberOfOutputs];

			
			for (int i = 0; i < numberOfInputs; i++)
			{
				string txt = entries[i].Text?.Trim().ToLower() ?? "false";
				
				inputs[i] = (txt == "1" || txt == "true");
			}
			
			for (int j = 0; j < numberOfOutputs; j++)
			{
				string txt = entries[numberOfInputs + j].Text?.Trim().ToLower() ?? "false";
				outputs[j] = (txt == "1" || txt == "true");
			}
			truthRows.Add(new TruthTableRow { Inputs = inputs, ExpectedOutputs = outputs });
		}

		return truthRows;
	}

    private void loadCanvasFromXml()
    {
        Canvas.Children.Clear();
        _cardMap.Clear();
        _connections.Clear();

        if (BindingContext is not LearnerViewModel lvm)
            return;

        // Load and render all cards
        var cards = lvm.LoadCards();
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
        var conns = lvm.LoadConnections();
        foreach (var conn in conns)
        {
            RestoreConnection(conn.FromId, conn.ToId, conn.TargetInputIndex);
        }

        UpdateCanvasSize();
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

		_dragEndCts?.Cancel();
		_dragEndCts = new CancellationTokenSource();

		Task.Delay(500, _dragEndCts.Token).ContinueWith(t =>
		{
			if (!t.IsCanceled)
			{
				var xmlService = new XmlStateService(statePath);
				xmlService.SetCardPosition(id, newX, newY);
			}
		}, TaskScheduler.FromCurrentSynchronizationContext());

		UpdateCanvasSize();
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

    private CardView CreateCardView(int id, GateTypeEnum type, double x, double y, bool enableOutputTap)
    {
        if (BindingContext is not LearnerViewModel lvm)
            throw new InvalidOperationException("BindingContext is not a PuzzleViewModel");
        var vm = new CardViewModel(lvm.GetXmlStateService(), id, type)
        {
            X = x,
            Y = y
        };

        var cv = new CardView { BindingContext = vm };

        cv.PositionChanged += OnCardMoved;

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

        Canvas.Children.Add(connectionWire);
        Canvas.Children.Add(hitArea);

        connection.HitArea = hitArea;
        _connections.Add(connection);
    }

	public class TruthTableRow
	{
		public bool[] Inputs { get; set; }
		public bool[] ExpectedOutputs { get; set; }
	}
}