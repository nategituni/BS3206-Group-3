using System.Collections.ObjectModel;
using System.ComponentModel;
using GroupProject.Model;
//placeholder

namespace GroupProject.ViewModel
{
    public class ChallengesViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Challenge> Challenges { get; set; }

        public ChallengesViewModel()
        {
            Challenges = new ObservableCollection<Challenge>
            {
                new Challenge { Name = "Logic Puzzle 1", Difficulty = "Easy", IsCompleted = true },
                new Challenge { Name = "Logic Puzzle 2", Difficulty = "Hard", IsCompleted = false },
                // Add more as needed
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}