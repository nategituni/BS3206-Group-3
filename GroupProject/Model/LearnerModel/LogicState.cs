namespace GroupProject.Model.LearnerModel;

using GroupProject.Model.LogicModel;

public class LogicState
{
	public List<string> Gates;
	public List<(int from, int to)> Connections;
	public bool HasDisconnectedGate { get; set; } = false;

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
			// Compare Gates order-independently by sorting.
			bool gatesEqual = Gates
				.OrderBy(g => g)
				.SequenceEqual(other.Gates.OrderBy(g => g));

			// Compare Connections order-independently.
			bool connectionsEqual = Connections
				.OrderBy(c => c.from)
				.ThenBy(c => c.to)
				.SequenceEqual(other.Connections.OrderBy(c => c.from).ThenBy(c => c.to));

			return gatesEqual && connectionsEqual;
		}
		return false;
	}

	public override int GetHashCode()
	{
		int hash = 17;

		// Process gates in a sorted order so that order doesn't affect the result.
		foreach (var gate in Gates.OrderBy(g => g))
		{
			hash = hash * 31 + gate.GetHashCode();
		}

		// Process connections in a sorted order.
		foreach (var connection in Connections.OrderBy(c => c.from).ThenBy(c => c.to))
		{
			hash = hash * 31 + connection.from.GetHashCode();
			hash = hash * 31 + connection.to.GetHashCode();
		}

		return hash;
	}

	public static void CalculateLogicState(List<bool> inputs)
	{
		
	}
}