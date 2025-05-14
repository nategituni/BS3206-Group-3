using GroupProject.ViewModels;

namespace GroupProject.View;

public partial class EditProfilePopup : ContentPage
{
    public EditProfilePopup(AccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
