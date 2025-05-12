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