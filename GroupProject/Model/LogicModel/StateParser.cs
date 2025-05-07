using System.Xml.Linq;

namespace GroupProject.Model.LogicModel
{
	public class StateParser
	{
		string xml = @"
		<State>
			<Cards>
				<InputCards>
					<ICard id='0' value='true'/>
					<ICard id='1' value='true'/>
					<ICard id='2' value='true'/>
					<ICard id='3' value='true'/>
				</InputCards>

				<LogicGateCards>
					<!-- id, type, input cards IDs -->
					<LogicGate id='9' gateType='and' input1='5' input2='6'/>
					<LogicGate id='4' gateType='xor' input1='0' input2='2'/>
					<LogicGate id='5' gateType='and' input1='0' input2='2'/>
					<LogicGate id='8' gateType='xor' input1='5' input2='6'/>
					<LogicGate id='6' gateType='xor' input1='1' input2='3'/>
					<LogicGate id='7' gateType='and' input1='1' input2='3'/>
					<LogicGate id='10' gateType='or' input1='9' input2='7'/>
				</LogicGateCards>

				<OutputCards>
					<!-- id, value card ID -->
					<OCard id='11' input1='10'/>
					<OCard id='12' input1='8'/>
					<OCard id='13' input1='4'/>
				</OutputCards>
			</Cards>
		</State>";

		public (List<IOCard> inputCards, List<LogicGateCard> logicGateCards, List<OutputCard> outputCards) parseCards()
		{
			XDocument doc = XDocument.Parse(xml);

			// Parse Input Cards
			List<IOCard> inputCards = doc.Descendants("ICard")
				.Select(x =>
				{
					var card = new IOCard();
					card.Id = (int)x.Attribute("id");
					card.SetValue(bool.Parse(x.Attribute("value").Value));
					return card;
				})
				.ToList();

				// Process Logic gate cards sequentially
				List<LogicGateCard> logicGateCards = new List<LogicGateCard>();
				var sortedLogicGateElements = doc.Descendants("LogicGate")
					.OrderBy(x => (int)x.Attribute("id"))
					.ToList();

				foreach (var element in sortedLogicGateElements)
				{
					int gateId = (int)element.Attribute("id");
					string gateType = (string)element.Attribute("gateType");
					int input1Id = (int)element.Attribute("input1");
					int input2Id = (int)element.Attribute("input2");

					var gate = new LogicGateCard(gateType);
					gate.Id = gateId;

					List<IOutputProvider> availableProviders = new List<IOutputProvider>();
					availableProviders.AddRange(inputCards);
					availableProviders.AddRange(logicGateCards);

					var provider1 = availableProviders.FirstOrDefault(p =>
						(p is IOCard io && io.Id == input1Id) ||
						(p is LogicGateCard lg && lg.Id == input1Id));

					var provider2 = availableProviders.FirstOrDefault(p =>
						(p is IOCard io && io.Id == input2Id) ||
						(p is LogicGateCard lg && lg.Id == input2Id));

					gate.Input1Card = provider1;
					gate.Input2Card = provider2;

					gate.CalculateOutput();

					logicGateCards.Add(gate);
				}

				// Parse Output Cards
				List<OutputCard> outputCards = doc.Descendants("OutputCards").Descendants("OCard")
					.Select(x =>
					{
						var outputCard = new OutputCard();
						outputCard.Id = int.Parse(x.Attribute("id").Value);
						int inputRefId = int.Parse(x.Attribute("input1").Value);

						// Look among all providers: input cards and logic gates.
						List<IOutputProvider> allProviders = new List<IOutputProvider>();
						allProviders.AddRange(inputCards);
						allProviders.AddRange(logicGateCards);
					
						var provider = allProviders.FirstOrDefault(p =>
							(p is IOCard io && io.Id == inputRefId) ||
							(p is LogicGateCard lg && lg.Id == inputRefId));

						outputCard.Input1Card = provider;
						if (provider != null)
						{
							// This sets the Output property from IOCard (via SetValue)
							outputCard.SetValue(provider.Output);
							// Also, update the property that you print.
							outputCard.Input1 = provider.Output;
						}
						return outputCard;
					})
					.ToList();
			
			return (inputCards, logicGateCards, outputCards);
		}

		public void PrintCards()
		{
			var (inputCards, logicGateCards, outputCards) = parseCards();

			foreach (var card in inputCards)
			{
				Console.WriteLine($"Input Card ID: {card.Id}, Value: {card.Output}");
			}

			foreach (var card in logicGateCards)
			{
				Console.WriteLine($"Logic Gate Card ID: {card.Id}, Type: {card.GateType}, Input1: {card.Input1}, Input2: {card.Input2}, Output: {card.Output}");
			}

			foreach (var card in outputCards)
			{
				Console.WriteLine($"Output Card ID: {card.Id}, Input1: {card.Input1}, Value: {card.Output}");
			}
		}
	}
}