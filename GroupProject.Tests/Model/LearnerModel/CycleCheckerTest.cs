using GroupProject.Model;
using GroupProject.Model.LearnerModel;
using GroupProject.Model.LogicModel;
using GroupProject.View;
using GroupProject.ViewModel;
using Xunit;

namespace GroupProject.Tests.Model.LearnerModel;

public class CycleCheckerTest
{
	[Fact]
	public void CheckForCycle_ShouldReturnTrue_WhenCycleExists()
	{
		// Arrange
		var state = new LogicState
		{
			Gates = new List<string> { "And", "Or", "Not" },
			Connections = new List<(int from, int to)>
			{
				(0, 1),
				(1, 2),
				(2, 0)
			}
		};

		// Act
		bool result = CycleChecker.CheckForCycle(state);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void CheckForCycle_ShouldReturnFalse_WhenNoCycleExists()
	{
		// Arrange
		var state = new LogicState
		{
			Gates = new List<string> { "And", "Or", "Not" },
			Connections = new List<(int from, int to)>
			{
				(0, 1),
				(1, 2)
			}
		};

		// Act
		bool result = CycleChecker.CheckForCycle(state);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void CheckForCycle_ShouldHandleDisconnectedGraph()
	{
		// Arrange
		var state = new LogicState
		{
			Gates = new List<string> { "And", "Or", "Not" },
			Connections = new List<(int from, int to)>
			{
				(0, 1)
			}
		};

		// Act
		bool result = CycleChecker.CheckForCycle(state);

		// Assert
		Assert.False(result);
	}
}