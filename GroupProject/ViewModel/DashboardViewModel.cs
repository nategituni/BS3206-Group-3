using System.Windows.Input;

namespace GroupProject.ViewModel
{
	public class DashboardViewModel
	{
		public ICommand GoToAccountCommand { get; }
		public ICommand GoToLogoutCommand { get; }
		public ICommand GoToSandboxCommand { get; }
		public ICommand GoToTrendingCommand { get; }
		public ICommand GoToLearnerCommand { get; }
		public ICommand GoToChallengesCommand { get; }

		public DashboardViewModel()
		{
			GoToAccountCommand = new Command(async () => await GoToAccountPageAsync());
			GoToLogoutCommand = new Command(async () => await GoToLogoutPageAsync());
			GoToSandboxCommand = new Command(async () => await GoToSandboxPageAsync());
			GoToTrendingCommand = new Command(async () => await GoToTrendingPageAsync());
			GoToLearnerCommand = new Command(async () => await GoToLearnerPageAsync());
			GoToChallengesCommand = new Command(async () => await GoToChallengesPageAsync());
		}

		private async Task GoToAccountPageAsync()
		{
			await Shell.Current.GoToAsync("///AccountPage");
		}

		private async Task GoToLogoutPageAsync()
		{
			Preferences.Remove("UserEmail");

			await Shell.Current.GoToAsync("//LoginPage");
		}

		private async Task GoToSandboxPageAsync()
		{
			await Shell.Current.GoToAsync("///PuzzlePage");
		}

		private async Task GoToTrendingPageAsync()
		{
			await Shell.Current.GoToAsync("///TrendingPage");
		}

		private async Task GoToLearnerPageAsync()
		{
			await Shell.Current.GoToAsync("///LearnerPage");
		}

		private async Task GoToChallengesPageAsync()
		{
			await Shell.Current.GoToAsync("///ChallengesPage");
		}
    }
}
