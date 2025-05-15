using System.Windows.Input;

namespace GroupProject.ViewModel
{
    public class DashboardViewModel
    {
        public ICommand GoToAccountCommand { get; }
        public ICommand GoToLogoutCommand { get; }
        public ICommand GoToSandboxCommand { get; }
        public ICommand GoToTrendingCommand { get; }

        public DashboardViewModel()
        {
            GoToAccountCommand = new Command(async () => await GoToAccountPageAsync());
            GoToLogoutCommand = new Command(async () => await GoToLogoutPageAsync());
            GoToSandboxCommand = new Command(async () => await GoToSandboxPageAsync());
            GoToTrendingCommand = new Command(async () => await GoToTrendingPageAsync());
        }

        private async Task GoToAccountPageAsync()
        {
            await Shell.Current.GoToAsync("///AccountPage");
        }

        private async Task GoToLogoutPageAsync()
        {
            await Shell.Current.GoToAsync("///LoginPage");
        }

        private async Task GoToSandboxPageAsync()
        {
            await Shell.Current.GoToAsync("///PuzzlePage");
        }

        private async Task GoToTrendingPageAsync()
        {
            await Shell.Current.GoToAsync("///TrendingPage");
        }
    }
}
