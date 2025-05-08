namespace GroupProject.Tests.Model.LogicModel
{
	using GroupProject.Model.LogicModel;
	public class IOCardTest
	{
		[Fact]
		public void TestIOCardAndOutputProvider()
		{
			// Arrange
			var ioCard = new IOCard { Id = 1 };

			// Act
			ioCard.SetValue(true);

			// Assert
			Assert.True(ioCard.Output);
		}
	}
}