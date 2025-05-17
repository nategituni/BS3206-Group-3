using GroupProject.ViewModel;

namespace GroupProject.View;

public partial class TrendingPage : ContentView
{
    public TrendingPage()
    {
        InitializeComponent();
        BindingContext = new TrendingPageViewModel();
    }
}