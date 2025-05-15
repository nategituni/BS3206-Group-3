namespace GroupProject.Model.LogicModel;

public class CardInfo
{
    public int OldId { get; set; }
    public int NewId { get; set; }
    public required string Type { get; set; } // "Input", "LogicGate", "Output"
    public List<int> Dependencies { get; } = new(); // older IDs of cards this one depends on.
}