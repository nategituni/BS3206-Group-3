using GroupProject.View;
namespace GroupProject.view;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
        Shell.Current.GoToAsync("//Login");
    }
}
