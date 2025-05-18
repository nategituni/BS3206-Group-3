using GroupProject.Model;
using GroupProject.View;
using GroupProject.ViewModel;
using GroupProject.Services;
using GroupProject.Model.LogicModel;
using GroupProject.Model.LearnerModel;
using Xunit;

namespace GroupProject.Tests.ViewModel;

public class LearnerPageViewModelTest
{
	private readonly string testFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestState.xml");

	[Fact]
	public void TestSaveStateToXml()
	{
		// Arrange
		var xmlService = new XmlStateService(testFilePath);
		var learnerViewModel = new LearnerViewModel();
		var logicState = new LogicState
		{
			Gates = new List<string> { "Input 1", "AND 1 2", "Output 3" }
		};

		// Act
		learnerViewModel.SaveStateToXml(logicState, xmlService);

		// Assert
		Assert.True(File.Exists(testFilePath));
		var doc = xmlService.Document;
		Assert.NotNull(doc);
		Assert.Equal(3, doc.Descendants("ICard").Count() + doc.Descendants("LogicGate").Count() + doc.Descendants("OCard").Count());
	}

	[Fact]
	public void TestGetXmlStateService()
	{
		// Arrange
		var learnerViewModel = new LearnerViewModel();

		// Act
		var xmlService = learnerViewModel.GetXmlStateService();

		// Assert
		Assert.NotNull(xmlService);
		Assert.Equal(testFilePath, xmlService.Document.BaseUri);
	}
}