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

                var provider = allProviders.FirstOrDefault(p =>
                    (p is IOCard io && io.Id == inputRefId) ||
                    (p is LogicGateCard lg && lg.Id == inputRefId));

                outputCard.Input1Card = provider;
                if (provider != null)
                {
                    // This sets the Output property from IOCard (via SetValue)
                    outputCard.SetValue(provider.Output);
                    outputCard.Input1 = provider.Output;
                }

                return outputCard;
            })
            .ToList();

        return (inputCards, logicGateCards, outputCards);
    }

    public void SaveCards(
        string filePath,
        List<IOCard> inputCards,
        List<LogicGateCard> logicGateCards,
        List<OutputCard> outputCards)
    {
        // 1) Topo‐sort gates by their gate→input dependencies
        var sortedGates = TopoSort(logicGateCards).ToList();

        // 2) Build oldId → newId map (1..N)
        var idMap = new Dictionary<int, int>();
        for (var i = 0; i < sortedGates.Count; i++)
            idMap[sortedGates[i].Id] = i + 1;

        // 3) Compose XML
        var root = new XElement("State",
            // ICards
            inputCards.Select(ic =>
                new XElement("ICard",
                    new XAttribute("id", ic.Id),
                    new XAttribute("value", ic.Output.ToString().ToLowerInvariant())
                )
            ),

            // LogicGate (re‐ID’d in topo order)
            sortedGates.Select(g =>
                new XElement("LogicGate",
                    new XAttribute("id", idMap[g.Id]),
                    new XAttribute("gateType", g.GateType),
                    new XAttribute("input1", MapProviderId(g.Input1Card, idMap)),
                    new XAttribute("input2", MapProviderId(g.Input2Card, idMap))
                )
            ),

            // OutputCards → OCard
            new XElement("OutputCards",
                outputCards.Select(oc =>
                    new XElement("OCard",
                        new XAttribute("id", oc.Id),
                        new XAttribute("input1", MapProviderId(oc.Input1Card, idMap))
                    )
                )
            )
        );

        new XDocument(root).Save(filePath);
    }

    /// <summary>
    ///     Fetches the un‐mapped Id of any provider.
    /// </summary>
    private static int GetProviderId(IOutputProvider prov)
    {
        if (prov is LogicGateCard lg) return lg.Id;
        if (prov is IOCard io) return io.Id;
        if (prov is OutputCard oc) return oc.Id;
        return 0;
    }

    /// <summary>
    ///     Maps an IOutputProvider → the correct XML id given the new gate idMap.
    /// </summary>
    private static int MapProviderId(IOutputProvider prov, Dictionary<int, int> idMap)
    {
        if (prov is LogicGateCard lg && idMap.TryGetValue(lg.Id, out var newId))
            return newId;
        // IOCard & OutputCard retain their original Id
        if (prov is IOCard io) return io.Id;
        if (prov is OutputCard oc) return oc.Id;
        return 0;
    }

    private static IEnumerable<LogicGateCard> TopoSort(IEnumerable<LogicGateCard> gates)
    {
        var deps = gates.ToDictionary(
            g => g.Id,
            g =>
            {
                var set = new HashSet<int>();
                if (g.Input1Card is LogicGateCard i1) set.Add(i1.Id);
                if (g.Input2Card is LogicGateCard i2) set.Add(i2.Id);
                return set;
            });

        var queue = new Queue<LogicGateCard>(
            gates.Where(g => deps[g.Id].Count == 0)
        );
        var sorted = new List<LogicGateCard>();
        var remaining = gates.ToDictionary(g => g.Id, g => g);

        while (queue.Count > 0)
        {
            var g = queue.Dequeue();
            sorted.Add(g);
            remaining.Remove(g.Id);

            foreach (var kv in deps.ToList())
                if (kv.Value.Remove(g.Id) && kv.Value.Count == 0 && remaining.ContainsKey(kv.Key))
                    queue.Enqueue(remaining[kv.Key]);
        }

        // Append any that remain (cycle)
        if (remaining.Count > 0)
            sorted.AddRange(remaining.Values);

        return sorted;
    }

    public void TwoBitAdderResultToConsole()
    {
        var (inputCards, logicGateCards, outputCards) = parseCards();

        Console.WriteLine("Inputs:");
        Console.WriteLine("A: " + (inputCards[1].Output ? "1" : "0") + (inputCards[0].Output ? "1" : "0"));
        Console.WriteLine("B: " + (inputCards[3].Output ? "1" : "0") + (inputCards[2].Output ? "1" : "0"));

        Console.WriteLine("Outputs:");
        Console.WriteLine("O: " + (outputCards[0].Output ? "1" : "0") + (outputCards[1].Output ? "1" : "0") +
                          (outputCards[2].Output ? "1" : "0"));
    }
}