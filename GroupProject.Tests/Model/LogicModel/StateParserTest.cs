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
	}

	[Theory]
	[InlineData(false, false, false, false, false, false, false)] // A1, A0, B1, B0, Carry, Sum1, Sum0
	[InlineData(false, false, false, true, false, false, true)]
	[InlineData(false, false, true, false, false, true, false)]
	[InlineData(false, false, true, true, false, true, true)]
	[InlineData(false, true, false, false, false, false, true)]
	[InlineData(false, true, false, true, false, true, false)]
	[InlineData(false, true, true, false, false, true, true)]
	[InlineData(false, true, true, true, true, false, false)]
	[InlineData(true, false, false, false, false, true, false)]
	[InlineData(true, false, false, true, false, true, true)]
	[InlineData(true, false, true, false, true, false, false)]
	[InlineData(true, false, true, true, true, false, true)]
	[InlineData(true, true, false, false, false, true, true)]
	[InlineData(true, true, false, true, true, false, false)]
	[InlineData(true, true, true, false, true, false, true)]
	[InlineData(true, true, true, true, true, true, false)]


	public void TestLogicWithTwoBitAdder(bool a1, bool a0, bool b1, bool b0, bool expectedCarry, bool expectedSum1, bool expectedSum0)
	{
		string testXml = $@"
		<Cards>
			<InputCards>
                <ICard id='0' value='{a0.ToString().ToLower()}'/>
                <ICard id='1' value='{a1.ToString().ToLower()}'/>
                <ICard id='2' value='{b0.ToString().ToLower()}'/>
                <ICard id='3' value='{b1.ToString().ToLower()}'/>
			</InputCards>

			<LogicGateCards>
				<LogicGate id='4' gateType='xor' input1='0' input2='2'/>
				<LogicGate id='5' gateType='and' input1='0' input2='2'/>
				<LogicGate id='6' gateType='xor' input1='1' input2='3'/>
				<LogicGate id='7' gateType='and' input1='1' input2='3'/>
				<LogicGate id='8' gateType='xor' input1='5' input2='6'/>
				<LogicGate id='9' gateType='and' input1='5' input2='6'/>
				<LogicGate id='10' gateType='or' input1='9' input2='7'/>
			</LogicGateCards>

			<OutputCards>
				<OCard id='11' input1='10'/>
				<OCard id='12' input1='8'/>
				<OCard id='13' input1='4'/>
			</OutputCards>
		</Cards>";

		XDocument mockDoc = XDocument.Parse(testXml);

		// Arrange
		var stateParser = new StateParser(mockDoc);

		// Act
		var (_, _, outputCards) = stateParser.parseCards();

		// Assert
		Assert.Equal(expectedCarry, outputCards[0].Output);
		Assert.Equal(expectedSum1, outputCards[1].Output);
		Assert.Equal(expectedSum0, outputCards[2].Output);
	}

}