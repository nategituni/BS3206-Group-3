using GroupProject.ViewModel;

namespace GroupProject.View;

public partial class TrendingPage : ContentPage
{
    public TrendingPage()
    {
        InitializeComponent();
        BindingContext = new TrendingPageViewModel();
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}