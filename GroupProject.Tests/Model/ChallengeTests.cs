using Xunit;
using GroupProject.Model;

namespace GroupProject.Tests.Model
{
    public class ChallengeTests
    {
        [Fact]
        public void Challenge_DefaultValues_ShouldBeCorrect()
        {
            var challenge = new Challenge();

            Assert.Null(challenge.Name);
            Assert.Null(challenge.Difficulty);
            Assert.False(challenge.IsCompleted);
            Assert.Null(challenge.Filename);
            Assert.Null(challenge.Description);
        }

        [Fact]
        public void Challenge_SetProperties_ShouldStoreValues()
        {
            var challenge = new Challenge
            {
                Name = "Test Challenge",
                Difficulty = "Easy",
                IsCompleted = true,
                Filename = "test.xml",
                Description = "This is a test challenge."
            };

            Assert.Equal("Test Challenge", challenge.Name);
            Assert.Equal("Easy", challenge.Difficulty);
            Assert.True(challenge.IsCompleted);
            Assert.Equal("test.xml", challenge.Filename);
            Assert.Equal("This is a test challenge.", challenge.Description);
        }
    }
}
