using System.Xml.Linq;

namespace GroupProject.Model.LogicModel;

public class StateParser
{
    private readonly XDocument _doc;

    // Default constructor loads the XML file from the current directory
    public StateParser()
    {
        var filepath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");
        _doc = XDocument.Load(filepath);
    }

    // Constructor for Testing purposes, allowing to pass a different XDocument
    public StateParser(XDocument doc)
    {
        _doc = doc;
    }

    public (List<IOCard> inputCards, List<LogicGateCard> logicGateCards, List<OutputCard> outputCards) parseCards()
    {
        var doc = _doc;

        // Parse Input Cards
        var inputCards = doc.Descendants("ICard")
            .Select(x =>
            {
                var card = new IOCard();
                card.Id = (int)x.Attribute("id");
                card.SetValue(bool.Parse(x.Attribute("value").Value));
                return card;
            })
            .ToList();

        // Process Logic gate cards sequentially
        var logicGateCards = new List<LogicGateCard>();
        var sortedLogicGateElements = doc.Descendants("LogicGate")
            .OrderBy(x => (int)x.Attribute("id"))
            .ToList();

        foreach (var element in sortedLogicGateElements)
        {
            var gateId = (int)element.Attribute("id");
            var gateType = (string)element.Attribute("gateType");
            var input1Id = (int)element.Attribute("input1");
            var input2Id = (int)element.Attribute("input2");

            var gate = new LogicGateCard(gateType);
            gate.Id = gateId;

            var availableProviders = new List<IOutputProvider>();
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
        var outputCards = doc.Descendants("OutputCards").Descendants("OCard")
            .Select(x =>
            {
                var outputCard = new OutputCard();
                outputCard.Id = int.Parse(x.Attribute("id").Value);
                var inputRefId = int.Parse(x.Attribute("input1").Value);

                // Look among all providers: input cards and logic gates.
                var allProviders = new List<IOutputProvider>();
                allProviders.AddRange(inputCards);
                allProviders.AddRange(logicGateCards);

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
	}
}