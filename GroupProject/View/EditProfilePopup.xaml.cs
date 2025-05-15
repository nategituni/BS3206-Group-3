using GroupProject.ViewModel;

namespace GroupProject.View;

public partial class EditProfilePopup : ContentPage
{
    public EditProfilePopup(AccountViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}