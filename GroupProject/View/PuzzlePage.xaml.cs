using System.Xml.Linq;
using GroupProject.Model.LogicModel;
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

    private readonly Dictionary<int, CardView> _cardMap = new();
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

                int id1 = GetCardId(g.Input1Card);
                int id2 = GetCardId(g.Input2Card);
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


    // ── AddGate helper ─────────────────────────────────────────────────

    private void AddGate(string gateType)
    {
        var id = _nextId++;
        var vm = new CardViewModel(id, gateType);
        var cv = new CardView { BindingContext = vm };
        AbsoluteLayout.SetLayoutBounds(cv, new Rect(vm.X, vm.Y, 120, 80));
        Canvas.Children.Add(cv);

        cv.PositionChanged += OnCardMoved;
        cv.OutputPortTapped += OnOutTapped;
        cv.InputPortTapped += OnInTapped;

        _cardMap[id] = cv;
        UpdateCanvasSize();
    }

    // ── Card drag ──────────────────────────────────────────────────────

    private void OnCardMoved(object s, PositionChangedEventArgs e)
    {
        if (_isDrawingWire) return;

        var cv = (CardView)s;
        var id = _cardMap.First(kvp => kvp.Value == cv).Key;

        foreach (var c in _connections.Where(c => c.SourceCardId == id || c.TargetCardId == id))
            UpdateConnectionLine(c);

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
            var dropX = _wireStartX + e.TotalX;
            var dropY = _wireStartY + e.TotalY;

            const double snapRadius = 15; // px threshold for “over” a port
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

                if (Distance(dropX, dropY, in1.X, in1.Y) < snapRadius)
                {
                    // snap & commit to input1
                    _tempWire.X2 = in1.X;
                    _tempWire.Y2 = in1.Y;
                    _connections.Add(new Connection
                    {
                        SourceCardId = _wireSrcId,
                        TargetCardId = tgtId,
                        TargetInputIndex = 1,
                        LineShape = _tempWire
                    });
                    connected = true;
                    break;
                }

                if (Distance(dropX, dropY, in2.X, in2.Y) < snapRadius)
                {
                    // snap & commit to input2
                    _tempWire.X2 = in2.X;
                    _tempWire.Y2 = in2.Y;
                    _connections.Add(new Connection
                    {
                        SourceCardId = _wireSrcId,
                        TargetCardId = tgtId,
                        TargetInputIndex = 2,
                        LineShape = _tempWire
                    });
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
    
    static int GetCardId(IOutputProvider p)
    {
        return p switch
        {
            LogicGateCard lg => lg.Id,
            IOCard        io => io.Id,
            _                => 0
        };
    }

    private void OnInTapped(object sender, int inputIndex)
    {
        if (!_isDrawingWire || _tempWire == null) return;

        var cv = (CardView)sender;
        var tgt = _cardMap.First(kvp => kvp.Value == cv).Key;
        var b = AbsoluteLayout.GetLayoutBounds(cv);

        _tempWire.X2 = b.X;
        _tempWire.Y2 = b.Y + b.Height * (inputIndex == 1 ? 0.25 : 0.75);

        _connections.Add(new Connection
        {
            SourceCardId = _wireSrcId,
            TargetCardId = tgt,
            TargetInputIndex = inputIndex,
            LineShape = _tempWire
        });

        Canvas.Children.Remove(_wireOverlay);
        _wireOverlay = null;
        _isDrawingWire = false;
        _tempDot = null;
        _tempWire = null;
    }

    // ── Redraw permanent wires ─────────────────────────────────────────
    
    void CreateConnection(int fromId, int toId, int inputIndex)
    {
        var line = new Line
        {
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };
        Canvas.Children.Add(line);

        var conn = new Connection
        {
            SourceCardId     = fromId,
            TargetCardId     = toId,
            TargetInputIndex = inputIndex,
            LineShape        = line
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
}