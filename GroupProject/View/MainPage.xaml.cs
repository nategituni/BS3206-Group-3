namespace GroupProject.View;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);

		Model.LogicModel.StateParser stateParser = new Model.LogicModel.StateParser();
		stateParser.parseCards();
		stateParser.TwoBitAdderResultToConsole(); // Temporary for printing the result of the 2 bit adder sample state
	}
}
