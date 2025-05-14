using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GroupProject.Services;

namespace GroupProject.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
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

        public ICommand LoginCommand { get; }
        public ICommand GoToRegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new Command(async () => await LoginAsync());
            GoToRegisterCommand = new Command(async () => await GoToRegisterAsync());
        }

        private async Task LoginAsync()
        {
            bool success = await AuthService.ValidateLoginAsync(Email, Password);

            if (success)
            {
                Preferences.Set("UserEmail", Email);
                await Shell.Current.GoToAsync("//Dashboard");
            }
            else
            {
                StatusMessage = "Invalid credentials";
            }
        }

        private async Task GoToRegisterAsync()
        {
            await Shell.Current.GoToAsync("//Register");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
