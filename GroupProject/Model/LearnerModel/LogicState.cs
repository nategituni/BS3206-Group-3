namespace GroupProject.Model.LearnerModel;

public class LogicState
{
	public List<string> Gates;
	public List<(int from, int to)> Connections;

	public LogicState()
	{
		Gates = new List<string>();
		Connections = new List<(int from, int to)>();
	}

	public LogicState Clone()
	{
		var clone = new LogicState();
		clone.Gates = new List<string>(this.Gates);
		clone.Connections = new List<(int from, int to)>(this.Connections);
		return clone;
	}

	public override bool Equals(object obj)
	{
		if (obj is LogicState other)
		{
			return Gates.SequenceEqual(other.Gates) && Connections.SequenceEqual(other.Connections);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Gates, Connections);
	}
}