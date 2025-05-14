using System.Windows.Input;

namespace GroupProject.ViewModels
{
    public class DashboardViewModel
    {
        public ICommand GoToAccountCommand { get; }
		public ICommand GoToLogoutCommand { get; }
		public ICommand GoToSandboxCommand { get; }

        public DashboardViewModel()
        {
            GoToAccountCommand = new Command(async () => await GoToAccountPageAsync());
			GoToLogoutCommand = new Command(async () => await GoToLogoutPageAsync());
			GoToSandboxCommand = new Command(async () => await GoToSandboxPageAsync());
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
    }
}
