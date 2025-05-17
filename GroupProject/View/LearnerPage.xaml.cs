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

		
		for (int i = 0; i < numberOfInputs; i++)
		{
			var inputHeader = new Label
			{
				Text = $"In {i + 1}",
				FontAttributes = FontAttributes.Bold,
				WidthRequest = 50,
				HorizontalTextAlignment = TextAlignment.Center
			};
			
			inputHeader.AutomationId = $"InputHeader_{i + 1}";

			headerLayout.Children.Add(inputHeader);
		}

		
		for (int i = 0; i < numberOfOutputs; i++)
		{
			var outputHeader = new Label
			{
				Text = $"Out {i + 1}",
				FontAttributes = FontAttributes.Bold,
				WidthRequest = 50,
				HorizontalTextAlignment = TextAlignment.Center
			};
			
			outputHeader.AutomationId = $"OutputHeader_{i + 1}";

			headerLayout.Children.Add(outputHeader);
		}

		truthTableLayout.Children.Add(headerLayout); 

		int numberOfRows = (int)Math.Pow(2, numberOfInputs);
		for (int row = 0; row < numberOfRows; row++)
		{
			var rowLayout = new StackLayout { Orientation = StackOrientation.Horizontal };

			
			for (int col = 0; col < numberOfInputs; col++)
			{
				var inputEntry = new Entry
				{
					Placeholder = "0 or 1",
					Keyboard = Keyboard.Numeric,
					WidthRequest = 50
				};
				
				inputEntry.AutomationId = $"InputEntry_Row{row}_Col{col}";

				rowLayout.Children.Add(inputEntry);
			}

			
			for (int col = 0; col < numberOfOutputs; col++)
			{
				var outputEntry = new Entry
				{
					Placeholder = "0 or 1",
					Keyboard = Keyboard.Numeric,
					WidthRequest = 50
				};
				
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

			foreach (var row in truthRows)
			{
				
				List<bool> inputValues = row.Inputs.ToList();

				var (inputCards, logicGateCards, outputCards) = EfficientCalculateLogicState(state, inputValues);

				var orderedOutputCards = outputCards.OrderBy(o => o.Id).ToList();

				for (int i = 0; i < row.ExpectedOutputs.Length; i++)
				{
					bool computedOutput = orderedOutputCards[i].Output;
					bool expectedOutput = row.ExpectedOutputs[i];

					
					if (computedOutput != expectedOutput)
					{
						return false;
					}
				}
			}
			
			return true;
		}

		static (List<IOCard> inputCards, List<LogicGateCard> logicGateCards, List<OutputCard> outputCards) EfficientCalculateLogicState(LogicState state, List<bool> inputs)
		{
			
			List<IOCard> inputCards = new List<IOCard>();
			int inputPos = 0;
			for (int i = 0; i < state.Gates.Count; i++)
			{
				if (state.Gates[i].StartsWith("Input", StringComparison.OrdinalIgnoreCase))
				{
					IOCard card = new IOCard();
					card.Id = i; 
					
					card.SetValue(inputs[inputPos]);
					inputCards.Add(card);
					inputPos++;
				}
			}

			List<LogicGateCard> logicGateCards = new List<LogicGateCard>();
			for (int i = 0; i < state.Gates.Count; i++)
			{
				if (!state.Gates[i].StartsWith("Input", StringComparison.OrdinalIgnoreCase) &&
					!state.Gates[i].StartsWith("Output", StringComparison.OrdinalIgnoreCase))
				{
					
					LogicGateCard gate = new LogicGateCard(state.Gates[i]);
					gate.Id = i;
					logicGateCards.Add(gate);
				}
			}

			List<OutputCard> outputCards = new List<OutputCard>();
			for (int i = 0; i < state.Gates.Count; i++)
			{
				if (state.Gates[i].StartsWith("Output", StringComparison.OrdinalIgnoreCase))
				{
					OutputCard card = new OutputCard();
					card.Id = i;
					outputCards.Add(card);
				}
			}

			List<IOutputProvider> availableProviders = new List<IOutputProvider>();
			availableProviders.AddRange(inputCards);
			availableProviders.AddRange(logicGateCards);
			
			var groupedConnections = state.Connections.GroupBy(c => c.to);
			foreach (var group in groupedConnections)
			{
				int targetIndex = group.Key;
				var connectionsForTarget = group.ToList();
				
				var provider1Id = connectionsForTarget[0].from;
				var provider1 = availableProviders.FirstOrDefault(p => p.Id == provider1Id);

				
				if (state.Gates[targetIndex].StartsWith("Output", StringComparison.OrdinalIgnoreCase))
				{
					var outputCard = outputCards.FirstOrDefault(o => o.Id == targetIndex);
					if (outputCard != null)
					{
						outputCard.Input1Card = provider1;
						if (provider1 != null)
						{
							
							outputCard.SetValue(provider1.Output);
							outputCard.Input1 = provider1.Output;
						}
					}
				}
				else 
				{
					var logicGate = logicGateCards.FirstOrDefault(g => g.Id == targetIndex);
					if (logicGate != null)
					{
						logicGate.Input1Card = provider1;
						
						if (connectionsForTarget.Count > 1)
						{
							var provider2Id = connectionsForTarget[1].from;
							var provider2 = availableProviders.FirstOrDefault(p => p.Id == provider2Id);
							logicGate.Input2Card = provider2;
						}
					}
				}
			}

			foreach (var gate in logicGateCards)
			{
				gate.CalculateOutput();
			}
			
			foreach (var outputCard in outputCards)
			{
				
				
				var connection = state.Connections.FirstOrDefault(c => c.to == outputCard.Id);
				if (connection != default)
				{
					var provider = availableProviders.FirstOrDefault(p => p.Id == connection.from);
					outputCard.Input1Card = provider;
					if (provider != null)
					{
						outputCard.SetValue(provider.Output);
						outputCard.Input1 = provider.Output;
					}
				}
			}

			return (inputCards, logicGateCards, outputCards);
		}

		void SearchForValidStateHeuristic()
		{
			LogicState initialState = BuildInitialState(numberOfInputs, numberOfOutputs);

			var frontier = new PriorityQueue<LogicState, int>();

			var visitedStates = new HashSet<LogicState>();

			(int cost, bool initialIsSolution) = Heuristic(initialState);

			if (initialIsSolution)
			{
				Dispatcher.Dispatch(() =>
				{
					lvm.SaveStateToXml(initialState, lvm.GetXmlStateService());
					Canvas.Children.Clear();
					_cardMap.Clear();
					_connections.Clear();
					AssignCardPositions();
					loadCanvasFromXml();
					DisplayAlert("Success", "Found a valid state!", "OK");
				});
				return;
			}

			frontier.Enqueue(initialState, cost);

			visitedStates.Add(initialState);

			while (frontier.Count > 0)
			{
				LogicState currentState = frontier.Dequeue();

				foreach (LogicState neighbor in GenerateNeighborStates(currentState))
				{
					if (!visitedStates.Contains(neighbor))
					{
						visitedStates.Add(neighbor);
						(int newCost, bool isSolution) = Heuristic(neighbor);

						if (!neighbor.HasDisconnectedGate && isSolution)
						{
							Dispatcher.Dispatch(() =>
							{
								Console.WriteLine($"VisitedStates count: {visitedStates.Count}"); // PERFORMANCE TESTING
								lvm.SaveStateToXml(neighbor, lvm.GetXmlStateService());
								Canvas.Children.Clear();
								_cardMap.Clear();
								_connections.Clear();
								AssignCardPositions();
								loadCanvasFromXml();
								DisplayAlert("Success", "Found a valid state!", "OK");
							});
							return;
						}
						frontier.Enqueue(neighbor, newCost);
					}
				}
			}

			Dispatcher.Dispatch(() =>
			{
				DisplayAlert("Failure", "No valid state found.", "OK");
			});
		}

		IEnumerable<LogicState> GenerateNeighborStates(LogicState currentState)
		{
			List<LogicState> neighbors = new List<LogicState>();

			foreach (var gateType in new[] { "And", "Xor", "Or", "Not", "Nand", "Nor", "Xnor" })  
			{
				LogicState newState = currentState.Clone();
				newState.Gates.Add(gateType);

				newState.HasDisconnectedGate = true;
				neighbors.Add(newState);
			}

			for (int sourceIndex = 0; sourceIndex < currentState.Gates.Count; sourceIndex++)
			{
				for (int targetIndex = 0; targetIndex < currentState.Gates.Count; targetIndex++)
				{
					if (sourceIndex == targetIndex)  
						continue;

					if (currentState.Connections.Any(c => c.from == sourceIndex && c.to == targetIndex))
						continue;

					string targetGate = currentState.Gates[targetIndex];
					string sourceGate = currentState.Gates[sourceIndex];

					if (targetGate.StartsWith("Input"))
						continue;

					if (sourceGate.StartsWith("Output"))
						continue;

					if ((targetGate.StartsWith("Output") || targetGate.StartsWith("Not")) && currentState.Connections.Count(c => c.to == targetIndex) >= 1)
						continue;

					if (!targetGate.StartsWith("Input") && !targetGate.StartsWith("Output") && currentState.Connections.Count(c => c.to == targetIndex) >= 2)
						continue;

					LogicState neighbor = currentState.Clone();
					neighbor.Connections.Add((sourceIndex, targetIndex));

					if (CycleChecker.CheckForCycle(neighbor))
						continue;

					neighbor.HasDisconnectedGate = !AreAllGatesConnected(neighbor);
					neighbors.Add(neighbor);
				}
			}

			return neighbors;
		}

		bool AreAllGatesConnected(LogicState state)
		{
			
			for (int i = 0; i < state.Gates.Count; i++)
			{
				
				string gateType = state.Gates[i];

				if (gateType.StartsWith("Input") || gateType.StartsWith("Output"))
					continue;

				
				int incomingCount = state.Connections.Count(c => c.to == i);
				int outgoingCount = state.Connections.Count(c => c.from == i);

				if (incomingCount < 1 || outgoingCount < 1)
					return false;
			}
			return true;
		}

		(int cost, bool isSolution) Heuristic(LogicState state)
		{
			var truthRows = GetTruthTableRowsFromSidebar();
			int totalMismatch = 0;
			int complexityPenalty = 0;

			foreach (var row in truthRows)
			{
				List<bool> inputValues = row.Inputs.ToList();

				var (inputCards, logicGateCards, outputCards) = EfficientCalculateLogicState(state, inputValues);

				var orderedOutputCards = outputCards.OrderBy(o => o.Id).ToList();

				for (int i = 0; i < row.ExpectedOutputs.Length; i++)
				{
					bool computedOutput = orderedOutputCards[i].Output;
					bool expectedOutput = row.ExpectedOutputs[i];


					if (computedOutput != expectedOutput)
					{
						totalMismatch += 100;
					}
				}
			}

			complexityPenalty += state.Gates.Count() * 10;
			complexityPenalty += state.Connections.Count() * 1;

			int cost = totalMismatch + complexityPenalty;

			bool isSolution = totalMismatch == 0;

			return (cost, isSolution);
		}
		SearchForValidStateHeuristic();
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