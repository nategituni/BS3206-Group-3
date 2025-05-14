namespace GroupProject.Models
{
    public class Puzzle
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string PuzzleName { get; set; }
        public string PuzzleData { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
