using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Xml.Linq;
using GroupProject.Model;

namespace GroupProject.ViewModel
{
    public class ChallengesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Challenge> Challenges { get; set; }

        private Challenge _selectedChallenge;
        public Challenge SelectedChallenge
        {
            get => _selectedChallenge;
            set
            {
                _selectedChallenge = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartChallengeCommand { get; }

        public ChallengesViewModel()
        {
            Challenges = new ObservableCollection<Challenge>();
            StartChallengeCommand = new Command<Challenge>(StartChallenge);
            LoadChallengesFromFiles(); // Initial load
        }

        public void LoadChallengesFromFiles()
        {
            Challenges.Clear();

            string challengeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Challenges");
            if (!Directory.Exists(challengeDir))
                return;

            foreach (var file in Directory.GetFiles(challengeDir, "*.xml"))
            {
                try
                {
                    var doc = XDocument.Load(file);
                    var metadata = doc.Descendants("ChallengeMetadata").FirstOrDefault();

                    bool isCompleted = false;
                    if (metadata != null)
                    {
                        var isCompletedElem = metadata.Element("IsCompleted");
                        if (isCompletedElem != null)
                            isCompleted = isCompletedElem.Value.Trim().ToLower() == "true";
                    }

                    Challenges.Add(new Challenge
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Filename = Path.GetFileName(file),
                        Difficulty = "Unknown", // Extendable if added to XML
                        Description = "Click to view challenge.", // Extendable too
                        IsCompleted = isCompleted
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load challenge {file}: {ex.Message}");
                }
            }
        }

        private async void StartChallenge(Challenge challenge)
        {
            if (challenge == null) return;

            try
            {
                var service = new Services.ChallengeService();
                service.LoadChallenge(challenge.Filename);

                await Shell.Current.GoToAsync($"///ChallengePage?filename={challenge.Filename}");
            }
            catch (Exception ex)
            {
await Application.Current.MainPage.DisplayAlert("Error", $"Failed to start challenge: {ex.Message}", "OK");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
