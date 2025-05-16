using GroupProject.Services;
using System.IO;
using System.Linq;
using Xunit;

namespace GroupProject.Tests.Services
{
    public class XmlStateServiceTests
    {
        private string GetTempFilePath() =>
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".xml");

        [Fact]
        public void Should_Create_New_XmlFile_If_NotExists()
        {
            // Arrange
            var tempFile = GetTempFilePath();

            // Act
            var service = new XmlStateService(tempFile);

            // Assert
            Assert.True(File.Exists(tempFile));
            Assert.NotNull(service.Document.Root.Element("InputCards"));
            Assert.NotNull(service.Document.Root.Element("LogicGateCards"));
            Assert.NotNull(service.Document.Root.Element("OutputCards"));

            // Cleanup
            File.Delete(tempFile);
        }

        [Fact]
        public void Should_Add_And_Delete_InputCard()
        {
            var tempFile = GetTempFilePath();
            var service = new XmlStateService(tempFile);

            service.AddInputCard(1, true, 10.5, 20.5);
            var ids = service.GetAllIds();
            Assert.Contains(1, ids);

            service.DeleteInputCard(1);
            ids = service.GetAllIds();
            Assert.DoesNotContain(1, ids);

            File.Delete(tempFile);
        }

        [Fact]
        public void Should_Update_InputCard_Value()
        {
            var tempFile = GetTempFilePath();
            var service = new XmlStateService(tempFile);

            service.AddInputCard(2, false, 0, 0);
            service.UpdateInputCardValue(2, true);

            var card = service.Document.Descendants("ICard")
                .FirstOrDefault(x => (int)x.Attribute("id") == 2);

            Assert.NotNull(card);
            Assert.Equal("true", card.Attribute("value")?.Value);

            File.Delete(tempFile);
        }

        [Fact]
        public void Should_Clear_State_File()
        {
            var tempFile = GetTempFilePath();
            var service = new XmlStateService(tempFile);

            service.AddInputCard(5, false, 0, 0);
            service.ClearStateFile();

            var ids = service.GetAllIds();
            Assert.Empty(ids);

            File.Delete(tempFile);
        }

        [Fact]
        public void Should_Update_Card_Position()
        {
            var tempFile = GetTempFilePath();
            var service = new XmlStateService(tempFile);

            service.AddInputCard(10, true, 0, 0);
            service.UpdateCardPosition(10, 99.99, 88.88);

            var card = service.Document.Descendants("ICard")
                .FirstOrDefault(x => (int)x.Attribute("id") == 10);

            Assert.NotNull(card);
            Assert.Equal("99.99", card.Attribute("xPos")?.Value);
            Assert.Equal("88.88", card.Attribute("yPos")?.Value);

            File.Delete(tempFile);
        }
    }
}
