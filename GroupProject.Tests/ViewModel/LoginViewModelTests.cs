using GroupProject.ViewModels;
using Xunit;

namespace GroupProject.Tests.ViewModels
{
	public class LoginViewModelTests
	{
		[Fact]
		public void Should_Raise_PropertyChanged_For_Email()
		{
			var vm = new LoginViewModel();
			bool raised = false;

			vm.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(vm.Email))
					raised = true;
			};

			vm.Email = "test@example.com";
			Assert.True(raised);
		}

		[Fact]
		public void Should_Raise_PropertyChanged_For_Password()
		{
			var vm = new LoginViewModel();
			bool raised = false;

			vm.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(vm.Password))
					raised = true;
			};

			vm.Password = "password123";
			Assert.True(raised);
		}

		[Fact]
		public void Should_Raise_PropertyChanged_For_StatusMessage()
		{
			var vm = new LoginViewModel();
			bool raised = false;

			vm.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(vm.StatusMessage))
					raised = true;
			};

			vm.StatusMessage = "Invalid credentials";
			Assert.True(raised);
		}
	}
}
