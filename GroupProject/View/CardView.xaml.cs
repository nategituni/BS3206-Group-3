namespace GroupProject.View;

public partial class CardView : ContentView
{
    private double _startX, _startY;

    public CardView()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Fires when the card moves (so wires can update).
    /// </summary>
    public event EventHandler<PositionChangedEventArgs> PositionChanged;

    /// <summary>
    ///     Which input port (1 or 2) was tapped.
    /// </summary>
    public event EventHandler<int> InputPortTapped;

    /// <summary>
    ///     Output port was tapped.
    /// </summary>
    public event EventHandler OutputPortTapped;

    // Drag the card by panning the frame
    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var view = this;
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                var bounds = AbsoluteLayout.GetLayoutBounds(view);
                _startX = bounds.X;
                _startY = bounds.Y;
                break;

            case GestureStatus.Running:
                var rect = AbsoluteLayout.GetLayoutBounds(view);
                var newX = _startX + e.TotalX;
                var newY = _startY + e.TotalY;
                AbsoluteLayout.SetLayoutBounds(view,
                    new Rect(newX, newY, rect.Width, rect.Height));
                PositionChanged?.Invoke(this,
                    new PositionChangedEventArgs(newX, newY));
                break;
        }
    }

    private void OnIn1Pressed(object s, EventArgs e)
    {
        InputPortTapped?.Invoke(this, 1);
    }

    private void OnIn2Pressed(object s, EventArgs e)
    {
        InputPortTapped?.Invoke(this, 2);
    }

    private void OnOutPressed(object sender, EventArgs e)
    {
        OutputPortTapped?.Invoke(this, EventArgs.Empty);
    }
}

public class PositionChangedEventArgs : EventArgs
{
    public PositionChangedEventArgs(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double X { get; }
    public double Y { get; }
}