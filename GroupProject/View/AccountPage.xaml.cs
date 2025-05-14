using GroupProject.ViewModels;

namespace GroupProject.View;

public partial class AccountPage : ContentPage
{
    public AccountPage()
    {
        InitializeComponent();
        BindingContext = new AccountViewModel();
    }
}
