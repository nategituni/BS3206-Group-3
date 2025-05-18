namespace GroupProject.Tests.Model.LogicModel
{
	using GroupProject.Model.LogicModel;

	public class LogicGateCardTest
	{
		[Theory]
		[InlineData(true, true, true)] // AND gate
		[InlineData(true, false, false)] // AND gate
		[InlineData(false, true, false)] // AND gate
		[InlineData(false, false, false)] // AND gate

		public void TestAndGate(bool input1, bool input2, bool expectedOutput)
		{
			// Arrange
			var andGate = new LogicGateCard("AND")
			{
				Input1 = input1,
				Input2 = input2
			};

			// Act
			andGate.CalculateOutput();

			// Assert
			Assert.Equal(expectedOutput, andGate.Output);
		}

		[Theory]
		[InlineData(true, true, true)] // OR gate
		[InlineData(true, false, true)] // OR gate
		[InlineData(false, true, true)] // OR gate
		[InlineData(false, false, false)] // OR gate

		public void TestOrGate(bool input1, bool input2, bool expectedOutput)
		{
			// Arrange
			var orGate = new LogicGateCard("OR")
			{
				Input1 = input1,
				Input2 = input2
			};

			// Act
			orGate.CalculateOutput();

			// Assert
			Assert.Equal(expectedOutput, orGate.Output);
		}

		[Theory]
		[InlineData(true, false)] // NOT gate
		[InlineData(false, true)] // NOT gate

		public void TestNotGate(bool input1, bool expectedOutput)
		{
			// Arrange
			var notGate = new LogicGateCard("NOT")
			{
				Input1 = input1
			};

			// Act
			notGate.CalculateOutput();

			// Assert
			Assert.Equal(expectedOutput, notGate.Output);
		}

		[Theory]
		[InlineData(true, true, false)] // XOR gate
		[InlineData(true, false, true)] // XOR gate
		[InlineData(false, true, true)] // XOR gate
		[InlineData(false, false, false)] // XOR gate

		public void TestXorGate(bool input1, bool input2, bool expectedOutput)
		{
			// Arrange
			var xorGate = new LogicGateCard("XOR")
			{
				Input1 = input1,
				Input2 = input2
			};

			// Act
			xorGate.CalculateOutput();

			// Assert
			Assert.Equal(expectedOutput, xorGate.Output);
		}

		[Theory]
		[InlineData(true, true, false)] // NAND gate
		[InlineData(true, false, true)] // NAND gate
		[InlineData(false, true, true)] // NAND gate
		[InlineData(false, false, true)] // NAND gate

		public void TestNandGate(bool input1, bool input2, bool expectedOutput)
		{
			// Arrange
			var nandGate = new LogicGateCard("NAND")
			{
				Input1 = input1,
				Input2 = input2
			};

			// Act
			nandGate.CalculateOutput();

			// Assert
			Assert.Equal(expectedOutput, nandGate.Output);
		}

		[Theory]
		[InlineData(true, true, false)] // NOR gate
		[InlineData(true, false, false)] // NOR gate
		[InlineData(false, true, false)] // NOR gate
		[InlineData(false, false, true)] // NOR gate

		public void TestNorGate(bool input1, bool input2, bool expectedOutput)
		{
			// Arrange
			var norGate = new LogicGateCard("NOR")
			{
				Input1 = input1,
				Input2 = input2
			};

			// Act
			norGate.CalculateOutput();

			// Assert
			Assert.Equal(expectedOutput, norGate.Output);
		}

		[Theory]
		[InlineData(true, true, true)] // XNOR gate
		[InlineData(true, false, false)] // XNOR gate
		[InlineData(false, true, false)] // XNOR gate
		[InlineData(false, false, true)] // XNOR gate

		public void TestXnorGate(bool input1, bool input2, bool expectedOutput)
		{
			// Arrange
			var xnorGate = new LogicGateCard("XNOR")
			{
				Input1 = input1,
				Input2 = input2
			};

			// Act
			xnorGate.CalculateOutput();

			// Assert
			Assert.Equal(expectedOutput, xnorGate.Output);
		}

		[Fact]
		public void TestGateTypeEnum()
		{
			// Arrange
			var gateCard = new LogicGateCard(GateTypeEnum.And);

			// Act
			var gateType = gateCard.GateType;

			// Assert
			Assert.Equal(GateTypeEnum.And, gateType);
		}

		[Fact]
		public void TestGateTypeString()
		{
			// Arrange
			var gateCard = new LogicGateCard("AND");

			// Act
			var gateType = gateCard.GateType;

			// Assert
			Assert.Equal(GateTypeEnum.And, gateType);
		}
	}
}