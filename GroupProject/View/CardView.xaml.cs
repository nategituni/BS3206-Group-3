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
    public event EventHandler<PositionChangedEventArgs> PositionChanged;

    /// <summary>
    ///     Which input port (1 or 2) was tapped.
    /// </summary>
    public event EventHandler<int> InputPortTapped;

    /// <summary>
    ///     Output port was tapped.
    /// </summary>
    public event EventHandler OutputPortTapped;


	// Delete button
	public event EventHandler DeleteRequested;

	private readonly string statePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");

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

		var xmlService = new XmlStateService(statePath);

		// Retrieve the card's ID using its BindingContext.
		if (BindingContext is CardViewModel viewModel)
		{
			int cardId = viewModel.Id;
			// Update the input card in the XML using the card's id and new value.
			xmlService.UpdateInputCardValue(cardId, e.Value);
		}
	}

	private void OnDeleteButtonClicked(object sender, EventArgs e)
	{
		DeleteRequested?.Invoke(this, EventArgs.Empty);

		    // Retrieve the view model from the BindingContext
		if (BindingContext is CardViewModel viewModel)
		{
			int cardID = viewModel.Id;
			var xmlService = new XmlStateService(statePath);

			// Delete from the XML file based on the gate type.
			if (viewModel.GateType.Equals("Input", StringComparison.OrdinalIgnoreCase))
			{
				xmlService.DeleteInputCard(cardID);
			}
			else if (viewModel.GateType.Equals("Output", StringComparison.OrdinalIgnoreCase))
			{
				xmlService.DeleteOutputCard(cardID);
			}
			else // assume any other type is a logic gate
			{
				xmlService.DeleteLogicGateCard(cardID);
			}
		}
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