using System.Windows.Input;

namespace GroupProject.ViewModels
{
    public class DashboardViewModel
    {
        public ICommand GoToAccountCommand { get; }

        public DashboardViewModel()
        {
            GoToAccountCommand = new Command(async () => await GoToAccountPageAsync());
        }

        private async Task GoToAccountPageAsync()
        {
            await Shell.Current.GoToAsync("///AccountPage");
        }
    }
}
