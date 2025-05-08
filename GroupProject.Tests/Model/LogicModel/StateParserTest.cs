namespace GroupProject.Tests.Model.LogicModel;

using System.Xml.Linq;
using GroupProject.Model.LogicModel;
public class StateParserTest
{
	[Fact]
	public void TestParseCards()
	{
		string testXml = @"
        <Root>
            <ICard id='1' value='true'/>
            <ICard id='2' value='false'/>
            <LogicGate id='3' gateType='AND' input1='1' input2='2'/>
            <OutputCards>
                <OCard id='4' input1='3'/>
            </OutputCards>
        </Root>";

		XDocument mockDoc = XDocument.Parse(testXml);

		// Arrange
		var stateParser = new StateParser(mockDoc);
		
		// Act
		var (inputCards, logicGateCards, outputCards) = stateParser.parseCards();
		
		// Assert
		Assert.NotNull(inputCards);
		Assert.NotNull(logicGateCards);
		Assert.NotNull(outputCards);
		
		// Additional assertions can be added based on expected values
	}
}