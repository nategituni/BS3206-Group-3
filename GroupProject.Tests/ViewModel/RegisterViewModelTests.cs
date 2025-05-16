using GroupProject.ViewModels;
using Xunit;

namespace GroupProject.Tests.ViewModels
{
    public class RegisterViewModelTests
    {
        [Fact]
        public void Should_Raise_PropertyChanged_For_FullName()
        {
            var vm = new RegisterViewModel();
            bool raised = false;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.FullName))
                    raised = true;
            };

            vm.FullName = "Test User";
            Assert.True(raised);
        }

        [Fact]
        public void Should_Enable_Register_When_Valid_Input()
        {
            var vm = new RegisterViewModel();

            vm.FullName = "John Doe";
            vm.Email = "john@example.com";
            vm.Password = "Password1!";
            vm.ConfirmPassword = "Password1!";

            Assert.True(vm.IsRegisterEnabled);
        }

        [Fact]
        public void Should_Disable_Register_When_Emails_Invalid()
        {
            var vm = new RegisterViewModel();

            vm.FullName = "John Doe";
            vm.Email = "notanemail";
            vm.Password = "Password1!";
            vm.ConfirmPassword = "Password1!";

            Assert.False(vm.IsRegisterEnabled);
        }

        [Fact]
        public void Should_Disable_Register_When_Passwords_Dont_Match()
        {
            var vm = new RegisterViewModel();

            vm.FullName = "John Doe";
            vm.Email = "john@example.com";
            vm.Password = "Password1!";
            vm.ConfirmPassword = "Mismatch1!";

            Assert.False(vm.IsRegisterEnabled);
        }

        [Fact]
        public void Should_Enable_VerifyMfa_When_Code_Is_NotEmpty()
        {
            var vm = new RegisterViewModel();

            vm.MfaCode = "123456";

            Assert.True(vm.IsVerifyMfaEnabled);
        }

        [Fact]
        public void Should_Disable_VerifyMfa_When_Code_Is_Empty()
        {
            var vm = new RegisterViewModel();

            vm.MfaCode = "";

            Assert.False(vm.IsVerifyMfaEnabled);
        }

        [Fact]
        public void Should_Raise_PropertyChanged_For_StatusMessage()
        {
            var vm = new RegisterViewModel();
            bool raised = false;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.StatusMessage))
                    raised = true;
            };

            vm.StatusMessage = "Error occurred.";
            Assert.True(raised);
        }

        [Fact]
        public void Should_Raise_PropertyChanged_For_Email()
        {
            var vm = new RegisterViewModel();
            bool raised = false;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.Email))
                    raised = true;
            };

            vm.Email = "new@example.com";
            Assert.True(raised);
        }

        [Fact]
        public void Should_Raise_PropertyChanged_For_Password()
        {
            var vm = new RegisterViewModel();
            bool raised = false;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.Password))
                    raised = true;
            };

            vm.Password = "Password1!";
            Assert.True(raised);
        }

        [Fact]
        public void Should_Raise_PropertyChanged_For_ConfirmPassword()
        {
            var vm = new RegisterViewModel();
            bool raised = false;

            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.ConfirmPassword))
                    raised = true;
            };

            vm.ConfirmPassword = "Password1!";
            Assert.True(raised);
        }

    }
}
