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

		[Fact]
		public void TestIOCardSetValue()
		{
			// Arrange
			var ioCard = new IOCard { Id = 1 };

			// Act
			ioCard.SetValue(false);

			// Assert
			Assert.False(ioCard.Output);
		}

		[Fact]
		public void TestIOCardId()
		{
			// Arrange
			var ioCard = new IOCard { Id = 1 };

			// Act
			int id = ioCard.Id;

			// Assert
			Assert.Equal(1, id);
		}
	}
}