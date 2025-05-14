using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GroupProject.Services;

namespace GroupProject.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private string _fullName;
        public string FullName
        {
            get => _fullName;
            set
            {
                _fullName = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        private string _password;
        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        private string _confirmPassword;
        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        private bool _isRegisterEnabled;
        public bool IsRegisterEnabled
        {
            get => _isRegisterEnabled;
            set
            {
                _isRegisterEnabled = value;
                OnPropertyChanged();
            }
        }

        private string _mfaCode;
        public string MfaCode
        {
            get => _mfaCode;
            set
            {
                _mfaCode = value;
                OnPropertyChanged();
                ValidateMfaForm();
            }
        }

        private bool _isMfaVisible;
        public bool IsMfaVisible
        {
            get => _isMfaVisible;
            set
            {
                _isMfaVisible = value;
                OnPropertyChanged();
            }
        }

        private bool _isVerifyMfaEnabled;
        public bool IsVerifyMfaEnabled
        {
            get => _isVerifyMfaEnabled;
            set
            {
                _isVerifyMfaEnabled = value;
                OnPropertyChanged();
            }
        }

        public ICommand RegisterCommand { get; }
        public ICommand VerifyMfaCommand { get; }
        public ICommand GoToLoginCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new Command(async () => await RegisterAsync());
            VerifyMfaCommand = new Command(async () => await VerifyMfaAsync());
            GoToLoginCommand = new Command(async () => await Shell.Current.GoToAsync("//Login"));
        }

        private void ValidateForm()
        {
            IsRegisterEnabled =
                !string.IsNullOrWhiteSpace(FullName) &&
                !string.IsNullOrWhiteSpace(Email) &&
                !string.IsNullOrWhiteSpace(Password) &&
                !string.IsNullOrWhiteSpace(ConfirmPassword) &&
                ValidationHelper.IsValidEmail(Email) &&
                ValidationHelper.IsValidPassword(Password) &&
                Password == ConfirmPassword;
        }

        private void ValidateMfaForm()
        {
            IsVerifyMfaEnabled = !string.IsNullOrWhiteSpace(MfaCode);
        }

        private async Task RegisterAsync()
        {
            StatusMessage = "";
            if (await AuthService.IsEmailTakenAsync(Email))
            {
                StatusMessage = "Email is already registered.";
                return;
            }

            if (await AuthService.IsUsernameTakenAsync(FullName))
            {
                StatusMessage = "Username already in use.";
                return;
            }

            bool registered = await AuthService.RegisterUserAsync(FullName, Email, Password);

            if (registered)
            {
                var sent = await AuthService.SendMfaCodeAsync(Email);
                if (sent)
                {
                    StatusMessage = "A verification code has been sent to your email. Please check your junk/spam folder.";
                    IsMfaVisible = true;
                }
                else
                {
                    StatusMessage = "Failed to send verification code.";
                }
            }
            else
            {
                StatusMessage = "An error occurred. Try again.";
            }
        }

        private async Task VerifyMfaAsync()
        {
            if (await AuthService.VerifyMfaCodeAsync(Email, MfaCode))
            {
                Preferences.Set("UserEmail", Email);
                await Shell.Current.GoToAsync("//Login");
            }
            else
            {
                StatusMessage = "Invalid verification code.";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
