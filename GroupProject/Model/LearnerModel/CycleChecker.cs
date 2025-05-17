namespace GroupProject.Model.LearnerModel;

public static class CycleChecker
{
	public static bool CheckForCycle(LogicState state)
	{
		int n = state.Gates.Count;

		int[] inDegree = new int[n];
		List<int>[] graph = new List<int>[n];
		for (int i = 0; i < n; i++)
		{
			graph[i] = new List<int>();
		}

		foreach (var connection in state.Connections)
		{
			if (connection.from < 0 || connection.from >= n ||
				connection.to < 0 || connection.to >= n)
			{
				continue;
			}

			graph[connection.from].Add(connection.to);
			inDegree[connection.to]++;
		}

		Queue<int> queue = new Queue<int>();
		for (int i = 0; i < n; i++)
		{
			if (inDegree[i] == 0)
				queue.Enqueue(i);
		}

		int removedCount = 0;

		while (queue.Count > 0)
		{
			int node = queue.Dequeue();
			removedCount++;

			foreach (int neighbor in graph[node])
			{
				inDegree[neighbor]--;
				if (inDegree[neighbor] == 0)
					queue.Enqueue(neighbor);
			}
		}
		return removedCount < n;
	}
}
