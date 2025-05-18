using GroupProject.Model;
using GroupProject.Model.LearnerModel;
using GroupProject.View;
using GroupProject.ViewModel;
using Xunit;

namespace GroupProject.Tests.Model.LearnerModel;

public class LogicStateTest
{
	[Fact]
	public void TestGettersSetters()
	{
		// Arrange
		var logicState = new LogicState();
		logicState.Gates.Add("And");
		logicState.Connections.Add((0, 1));

		// Act
		var gates = logicState.Gates;
		var connections = logicState.Connections;

		// Assert
		Assert.Single(gates);
		Assert.Equal("And", gates[0]);
		Assert.Single(connections);
		Assert.Equal((0, 1), connections[0]);
	}

	[Fact]
	public void TestClone()
	{
		// Arrange
		var logicState = new LogicState();
		logicState.Gates.Add("And");
		logicState.Connections.Add((0, 1));

		// Act
		var clonedState = logicState.Clone();

		// Assert
		Assert.NotSame(logicState, clonedState);
		Assert.Equal(logicState.Gates, clonedState.Gates);
		Assert.Equal(logicState.Connections, clonedState.Connections);
	}

	[Fact]
	public void TestEquals()
	{
		// Arrange
		var logicState1 = new LogicState();
		logicState1.Gates.Add("And");
		logicState1.Connections.Add((0, 1));

		var logicState2 = new LogicState();
		logicState2.Gates.Add("And");
		logicState2.Connections.Add((0, 1));

		var logicState3 = new LogicState();
		logicState3.Gates.Add("Or");
		logicState3.Connections.Add((1, 0));

		// Act
		bool areEqual1 = logicState1.Equals(logicState2);
		bool areEqual2 = logicState1.Equals(logicState3);

		// Assert
		Assert.True(areEqual1);
		Assert.False(areEqual2);
	}

	[Fact]
	public void TestEqualsWithDifferentOrder()
	{
		// Arrange
		var logicState1 = new LogicState();
		logicState1.Gates.Add("And");
		logicState1.Gates.Add("Or");
		logicState1.Connections.Add((0, 1));

		var logicState2 = new LogicState();
		logicState2.Gates.Add("Or");
		logicState2.Connections.Add((0, 1));
		logicState2.Gates.Add("And");

		// Act
		bool areEqual = logicState1.Equals(logicState2);

		// Assert
		Assert.True(areEqual);
	}

	[Fact]
	public void TestGetHashCode()
	{
		// Arrange
		var logicState1 = new LogicState();
		logicState1.Gates.Add("And");
		logicState1.Connections.Add((0, 1));

		var logicState2 = new LogicState();
		logicState2.Gates.Add("And");
		logicState2.Connections.Add((0, 1));

		var logicState3 = new LogicState();
		logicState3.Gates.Add("Or");
		logicState3.Connections.Add((1, 0));

		// Act
		int hashCode1 = logicState1.GetHashCode();
		int hashCode2 = logicState2.GetHashCode();
		int hashCode3 = logicState3.GetHashCode();

		// Assert
		Assert.Equal(hashCode1, hashCode2);
		Assert.NotEqual(hashCode1, hashCode3);
	}

	[Fact]
	public void TestGetHashCodeWithDifferentOrder()
	{
		// Arrange
		var logicState1 = new LogicState();
		logicState1.Gates.Add("And");
		logicState1.Gates.Add("Or");
		logicState1.Connections.Add((0, 1));

		var logicState2 = new LogicState();
		logicState2.Gates.Add("Or");
		logicState2.Connections.Add((0, 1));
		logicState2.Gates.Add("And");

		// Act
		int hashCode1 = logicState1.GetHashCode();
		int hashCode2 = logicState2.GetHashCode();

		// Assert
		Assert.Equal(hashCode1, hashCode2);
	}

	[Fact]
	public void TestHasDisconnectedGateGetter()
	{
		// Arrange
		var logicState = new LogicState();
		logicState.Gates.Add("And");
		logicState.Connections.Add((0, 1));
		logicState.HasDisconnectedGate = true;

		// Act
		bool hasDisconnectedGate = logicState.HasDisconnectedGate;

		// Assert
		Assert.True(hasDisconnectedGate);
	}
}