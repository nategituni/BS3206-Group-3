namespace GroupProject.Model.LogicModel
{
	public class LogicGateCard : Card, IOutputProvider
	{
		public string GateType { get; set; }
		public bool Input1 { get; set; }
		public IOutputProvider Input1Card { get; set; }
		public bool Input2 { get; set; }
		public IOutputProvider Input2Card { get; set; }
		public bool Output { get; set; }

		public LogicGateCard(string gateType)
		{
			GateType = gateType;
		}

		public void CalculateOutput()
		{
			if (Input1Card != null)
			{
				Input1 = Input1Card.Output;
			}

			if (Input2Card != null)
			{
				Input2 = Input2Card.Output;
			}
	
			switch (GateType.ToLower())
			{
				case "and":
					Output = Input1 && Input2;
					break;
				case "or":
					Output = Input1 || Input2;
					break;
				case "not":
					Output = !Input1;
					break;
				case "xor":
					Output = Input1 ^ Input2;
					break;
				case "nand":
					Output = !(Input1 && Input2);
					break;
				case "nor":
					Output = !(Input1 || Input2);
					break;
				case "xnor":
					Output = !(Input1 ^ Input2);
					break;
				default:
					throw new InvalidOperationException("Invalid gate type");
			}
		}
	}
}