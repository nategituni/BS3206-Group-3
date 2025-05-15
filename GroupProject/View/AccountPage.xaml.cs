using GroupProject.ViewModel;

namespace GroupProject.View;

public partial class AccountPage : ContentPage
{
    public AccountPage()
    {
        InitializeComponent();
        BindingContext = new AccountViewModel();
    }

    private async void OnBackToDashboardClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///DashboardPage");
    }
}