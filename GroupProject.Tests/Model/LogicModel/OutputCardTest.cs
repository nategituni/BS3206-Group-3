using GroupProject.Model.LogicModel;
using Xunit;

namespace GroupProject.Tests.Model.LogicModel;

public class OutputCardTest
{
	[Fact]
	public void SetValue_ShouldSetOutput()
	{
		// Arrange
		var outputCard = new OutputCard();
		bool expectedValue = true;

		// Act
		outputCard.SetValue(expectedValue);

		// Assert
		Assert.Equal(expectedValue, outputCard.Output);
	}

	[Fact]
	public void SetValue_ShouldNotSetInput1()
	{
		// Arrange
		var outputCard = new OutputCard();
		bool expectedValue = true;

		// Act
		outputCard.SetValue(expectedValue);

		// Assert
		Assert.NotEqual(expectedValue, outputCard.Input1);
	}
}