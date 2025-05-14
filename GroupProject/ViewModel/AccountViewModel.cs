using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Input;
using Microsoft.Maui.Storage;
using GroupProject.Services;
using GroupProject.Models;

namespace GroupProject.ViewModels
{
    public class AccountViewModel : INotifyPropertyChanged
    {
        private ImageSource _profileImageSource;
        public ImageSource ProfileImageSource
        {
            get => _profileImageSource;
            set
            {
                _profileImageSource = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Puzzle> Puzzles { get; set; } = new();

        public ICommand UpdatePictureCommand { get; }
        public ICommand DeletePuzzleCommand { get; }

        public AccountViewModel()
        {
            UpdatePictureCommand = new Command(async () => await UpdatePictureAsync());
            DeletePuzzleCommand = new Command<Puzzle>(async (puzzle) => await DeletePuzzleAsync(puzzle));

            LoadProfileImageAsync();
            LoadPuzzlesAsync();
        }

        private async void LoadProfileImageAsync()
        {
            try
            {
                string email = Preferences.Get("UserEmail", null);
                if (string.IsNullOrWhiteSpace(email))
                {
                    await Shell.Current.DisplayAlert("Error", "Email not found in Preferences", "OK");
                    ProfileImageSource = "default_avatar.png";
                    return;
                }

                var base64 = await AuthService.GetProfilePictureAsync(email);
                if (!string.IsNullOrEmpty(base64))
                {
                    byte[] imageBytes = Convert.FromBase64String(base64);
                    ProfileImageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                }
                else
                {
                    ProfileImageSource = "default_avatar.png";
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Crash", $"Failed to load profile image: {ex.Message}", "OK");
                ProfileImageSource = "default_avatar.png";
            }
        }

        private async Task UpdatePictureAsync()
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a profile picture",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(imageBytes);

                string email = Preferences.Get("UserEmail", null);

                if (email != null)
                {
                    await AuthService.UpdateProfilePictureAsync(email, base64);
                    ProfileImageSource = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                    await Shell.Current.DisplayAlert("Done", "Profile picture updated!", "OK");
                }
            }
        }

        private async void LoadPuzzlesAsync()
        {
            try
            {
                string email = Preferences.Get("UserEmail", null);
                if (string.IsNullOrWhiteSpace(email)) return;

                int userId = await AuthService.GetUserIdByEmailAsync(email);
                var userPuzzles = await PuzzleService.GetUserPuzzlesAsync(userId);

                Puzzles.Clear();
                foreach (var puzzle in userPuzzles)
                    Puzzles.Add(puzzle);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to load puzzles: {ex.Message}", "OK");
            }
        }

        private async Task DeletePuzzleAsync(Puzzle puzzle)
        {
            if (puzzle == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Confirm", $"Delete puzzle '{puzzle.PuzzleName}'?", "Yes", "No");
            if (!confirm) return;

            await PuzzleService.DeletePuzzleAsync(puzzle.Id);
            Puzzles.Remove(puzzle);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
