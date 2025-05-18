using Xunit;
using GroupProject.ViewModel;
using GroupProject.Model;
using System.IO;
using System.Xml.Linq;
using System.Linq;

namespace GroupProject.Tests.ViewModel
{
    public class ChallengesViewModelTests
    {
        private readonly string _challengeDir;

        public ChallengesViewModelTests()
        {
            _challengeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Challenges");
            Directory.CreateDirectory(_challengeDir);
        }

        [Fact]
        public void LoadChallengesFromFiles_ShouldLoadValidChallenges()
        {
            // Arrange
            var testFile = Path.Combine(_challengeDir, "testchallenge.xml");
            var doc = new XDocument(
                new XElement("Challenge",
                    new XElement("ChallengeMetadata",
                        new XElement("IsCompleted", "true")
                    )
                )
            );
            doc.Save(testFile);

            // Act
            var vm = new ChallengesViewModel();

            // Assert
            Assert.NotEmpty(vm.Challenges);
            var challenge = vm.Challenges.FirstOrDefault(c => c.Filename == "testchallenge.xml");
            Assert.NotNull(challenge);
            Assert.Equal("testchallenge", challenge.Name);
            Assert.True(challenge.IsCompleted);
        }

        [Fact]
        public void SelectedChallenge_SetAndGet_ShouldWorkCorrectly()
        {
            var vm = new ChallengesViewModel();
            var challenge = new Challenge { Name = "Mock", Difficulty = "Easy" };

            vm.SelectedChallenge = challenge;

            Assert.Equal("Mock", vm.SelectedChallenge.Name);
        }
    }
}
