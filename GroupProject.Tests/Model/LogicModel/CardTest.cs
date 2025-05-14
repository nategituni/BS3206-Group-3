namespace GroupProject.Tests.Model.LogicModel
{
	using GroupProject.Model.LogicModel;

	public class CardTest
	{
		[Fact]
		public void TestCardId()
		{
			// Arrange
			var card = new Card { Id = 1 };

			// Act
			int id = card.Id;

			// Assert
			Assert.Equal(1, id);
		}
	}
}