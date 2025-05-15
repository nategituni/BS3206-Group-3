using GroupProject.Model.LogicModel;
using GroupProject.Services;
using GroupProject.ViewModel;

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
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    /// <summary>
    ///     Output port was tapped.
    /// </summary>
    public event EventHandler? OutputPortTapped;


    // Delete button
    public event EventHandler? DeleteRequested;

    private async void OnCardTapped(object sender, EventArgs e)
    {
        if (Parent is Layout layout)
        {
            layout.Children.Remove(this);
            layout.Children.Add(this);
        }

        DeleteButton.IsVisible = true;

        await Task.Delay(1000);

        DeleteButton.IsVisible = false;
    }

    private void OnInputValueToggled(object sender, ToggledEventArgs e)
    {
        InputValueLabel.Text = e.Value ? "1" : "0";

        // Retrieve the card's ID using its BindingContext.
        if (BindingContext is CardViewModel viewModel)
            viewModel.UpdateInputCardValue(e.Value);
    }

    private void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        DeleteRequested?.Invoke(this, EventArgs.Empty);

        // Retrieve the view model from the BindingContext
        if (BindingContext is CardViewModel viewModel)
            viewModel.Delete();
    }

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

    private void OnOutPressed(object sender, EventArgs e)
    {
        OutputPortTapped?.Invoke(this, EventArgs.Empty);
    }
}

public class PositionChangedEventArgs(double x, double y) : EventArgs
{
    public double X { get; } = x;
    public double Y { get; } = y;
}