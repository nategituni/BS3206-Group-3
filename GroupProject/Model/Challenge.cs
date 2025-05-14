namespace GroupProject.Model
{
    public class Challenge
    {
        public string Name { get; set; }
        public string Difficulty { get; set; } // e.g. "Easy", "Medium", "Hard"
        public bool IsCompleted { get; set; } = false;
        }
    }
