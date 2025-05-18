using System.Xml.Linq;
using GroupProject.Model.LogicModel;
using GroupProject.Model.Utilities;
using GroupProject.Services;
using GroupProject.View;

namespace GroupProject.ViewModel;

public class PuzzleViewModel
{
    private readonly string _statePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");
    private readonly XmlStateService _xmlService;
    
    public string? CurrentChallengeFilename { get; set; }


    public PuzzleViewModel()
    {
        _xmlService = new XmlStateService(_statePath);
    }

    public void ClearState()
    {
        _xmlService.ClearStateFile();
    }

    public List<int> GetAllIds()
    {
        return _xmlService.GetAllIds();
    }

    public List<CardData> LoadCards()
    {
        var doc = _xmlService.Document;
        var cards = new List<CardData>();

        cards.AddRange(doc.Descendants("InputCards").Elements("ICard").Select(e => new CardData
        {
            Id = XmlHelper.GetAttrInt(e, "id"),
            GateType = GateTypeEnum.Input,
            X = XmlHelper.GetAttrDouble(e, "xPos"),
            Y = XmlHelper.GetAttrDouble(e, "yPos"),
            IsLocked = XmlHelper.GetAttrBool(e, "locked")
        }));

        cards.AddRange(doc.Descendants("OutputCards").Elements("OCard").Select(e => new CardData
        {
            Id = XmlHelper.GetAttrInt(e, "id"),
            GateType = GateTypeEnum.Output,
            X = XmlHelper.GetAttrDouble(e, "xPos"),
            Y = XmlHelper.GetAttrDouble(e, "yPos"),
            IsLocked = XmlHelper.GetAttrBool(e, "locked")
        }));

        cards.AddRange(doc.Descendants("LogicGateCards").Elements("LogicGate").Select(e =>
        {
            var typeStr = XmlHelper.GetAttrString(e, "gateType");
            var type = Enum.TryParse<GateTypeEnum>(typeStr, true, out var parsed)
                ? parsed
                : GateTypeEnum.And; // fallback or handle error

            return new CardData
            {
                Id = XmlHelper.GetAttrInt(e, "id"),
                GateType = type,
                X = XmlHelper.GetAttrDouble(e, "xPos"),
                Y = XmlHelper.GetAttrDouble(e, "yPos")
            };
        }));

        return cards;
    }

    public List<ConnectionData> LoadConnections()
    {
        var doc = _xmlService.Document;
        var conns = new List<ConnectionData>();

        // OutputCards
        conns.AddRange(doc.Descendants("OutputCards").Elements("OCard").Select(e =>
        {
            var toId = XmlHelper.GetAttrInt(e, "id");
            var fromId = XmlHelper.GetAttrInt(e, "input1");

            return fromId == 0
                ? null
                : new ConnectionData
                {
                    FromId = fromId,
                    ToId = toId,
                    TargetInputIndex = 1
                };
        }).Where(c => c != null)!);

        // LogicGateCards
        conns.AddRange(doc.Descendants("LogicGateCards").Elements("LogicGate").SelectMany(e =>
        {
            var toId = XmlHelper.GetAttrInt(e, "id");
            var from1 = XmlHelper.GetAttrInt(e, "input1");
            var from2 = XmlHelper.GetAttrInt(e, "input2");

            var list = new List<ConnectionData>();
            if (from1 != 0) list.Add(new ConnectionData { FromId = from1, ToId = toId, TargetInputIndex = 1 });
            if (from2 != 0) list.Add(new ConnectionData { FromId = from2, ToId = toId, TargetInputIndex = 2 });
            return list;
        }));

        return conns;
    }


    public void AddGate(int id, GateTypeEnum type, double x, double y)
    {
        _xmlService.AddCard(id, type.ToString(), x, y);
    }

    public async Task SaveAsync(int userId, string puzzleName)
    {
        await PuzzleService.SavePuzzleAsync(userId, puzzleName);
    }

    public void UpdateCardPosition(int id, double x, double y)
    {
        _xmlService.SetCardPosition(id, x, y);
    }

    public void ConnectCards(int fromId, int toId, int inputIndex)
    {
        _xmlService.SetCardInput(toId, inputIndex, fromId);
    }

    public (bool success, Dictionary<int, CardView>) ReshuffleIds(Dictionary<int, CardView> cardMap,
        List<Connection> connections)
    {
        // 1. Gather all card info from the XML.
        var cards = new List<CardInfo>();

        var xmlService = _xmlService;
        var doc = xmlService.Document;

        // Assume _doc is your XML document that holds the state.
        // Input cards (have no dependency)
        foreach (var elem in doc.Descendants("InputCards").Elements("ICard"))
        {
            var oldId = (int)elem.Attribute("id");
            cards.Add(new CardInfo { OldId = oldId, Type = "Input" });
        }

        // Logic gate cards (depend on two inputs possibly).
        foreach (var elem in doc.Descendants("LogicGateCards").Elements("LogicGate"))
        {
            var oldId = (int)elem.Attribute("id");
            var info = new CardInfo { OldId = oldId, Type = "LogicGate" };

            // For each input attribute, add dependency if value is nonzero.
            var input1 = (int)elem.Attribute("input1");
            var input2 = (int)elem.Attribute("input2");
            if (input1 != 0)
                info.Dependencies.Add(input1);
            if (input2 != 0)
                info.Dependencies.Add(input2);

            cards.Add(info);
        }

        // Output cards (depend on one input).
        foreach (var elem in doc.Descendants("OutputCards").Elements("OCard"))
        {
            var oldId = (int)elem.Attribute("id");
            var info = new CardInfo { OldId = oldId, Type = "Output" };
            var input1 = (int)elem.Attribute("input1");
            if (input1 != 0)
                info.Dependencies.Add(input1);
            cards.Add(info);
        }

        // Compute in-degrees for each card.
        var inDegree = new Dictionary<int, int>();
        foreach (var card in cards) inDegree[card.OldId] = 0;
        foreach (var card in cards)
        foreach (var _ in card.Dependencies)
            // Increase in-degree for the card that depends on something.
            // (Here, each card's dependency is not about being depended upon; rather, the card
            // itself should have an in-degree corresponding to its number of dependencies.)
            inDegree[card.OldId]++;

        // Start with cards that have zero in-degree.
        var ready = new Queue<CardInfo>(cards.Where(c => inDegree[c.OldId] == 0));
        var sorted = new List<CardInfo>();

        while (ready.Count > 0)
        {
            var card = ready.Dequeue();
            sorted.Add(card);

            // For every card in the overall list that depends on this card,
            // decrement its in-degree.
            foreach (var dependent in cards.Where(c => c.Dependencies.Contains(card.OldId)))
            {
                inDegree[dependent.OldId]--;
                if (inDegree[dependent.OldId] == 0)
                    ready.Enqueue(dependent);
            }
        }

        // If there is a cycle, sorted.Count will not equal cards.Count
        if (sorted.Count != cards.Count)
        {
            return (false, cardMap);
        }

        // 3. Assign new IDs in the sorted order.
        var newId = 1;
        var idMapping = new Dictionary<int, int>(); // mapping old -> new
        foreach (var card in sorted)
        {
            card.NewId = newId;
            idMapping[card.OldId] = newId;
            newId++;
        }

        // 4. Update the XML: 
        // Update input cards
        foreach (var elem in doc.Descendants("InputCards").Elements("ICard"))
        {
            var oldId = (int)elem.Attribute("id");
            if (idMapping.ContainsKey(oldId)) elem.SetAttributeValue("id", idMapping[oldId]);
        }

        // Update logic gate cards (update id, input1, input2)
        foreach (var elem in doc.Descendants("LogicGateCards").Elements("LogicGate"))
        {
            var oldId = (int)elem.Attribute("id");
            if (idMapping.ContainsKey(oldId))
            {
                elem.SetAttributeValue("id", idMapping[oldId]);

                var input1 = (int)elem.Attribute("input1");
                var input2 = (int)elem.Attribute("input2");
                if (input1 != 0 && idMapping.ContainsKey(input1))
                    elem.SetAttributeValue("input1", idMapping[input1]);
                if (input2 != 0 && idMapping.ContainsKey(input2))
                    elem.SetAttributeValue("input2", idMapping[input2]);
            }
        }

        // Update output cards (update id and input1)
        foreach (var elem in doc.Descendants("OutputCards").Elements("OCard"))
        {
            var oldId = (int)elem.Attribute("id");
            if (idMapping.ContainsKey(oldId))
            {
                elem.SetAttributeValue("id", idMapping[oldId]);
                var input1 = (int)elem.Attribute("input1");
                if (input1 != 0 && idMapping.ContainsKey(input1))
                    elem.SetAttributeValue("input1", idMapping[input1]);
            }
        }

        // Save the updated XML
        SaveStateToDisk();

        // 5. Update the in‑memory CardView objects on your canvas.
        // Here we update each CardView's BindingContext (a CardViewModel).
        foreach (var cardView in cardMap.Values)
            if (cardView.BindingContext is CardViewModel vm)
                if (idMapping.ContainsKey(vm.Id))
                    vm.Id = idMapping[vm.Id];


        var newCardMap = new Dictionary<int, CardView>();
        foreach (var card in cardMap.Values)
            if (card.BindingContext is CardViewModel vm)
                newCardMap[vm.Id] = card;

        cardMap = newCardMap;

        foreach (var connection in connections)
        {
            if (idMapping.ContainsKey(connection.SourceCardId))
                connection.SourceCardId = idMapping[connection.SourceCardId];

            if (idMapping.ContainsKey(connection.TargetCardId))
                connection.TargetCardId = idMapping[connection.TargetCardId];
        }

        xmlService.PrintStateFile();
        return (true, cardMap);
    }

    public void SaveStateToDisk()
    {
        _xmlService.Save();
    }

    public XmlStateService GetXmlStateService()
    {
        return _xmlService;
    }

    public List<(int OutputCardId, bool Value)> EvaluateOutputs()
    {
        var parser = new StateParser();
        var (_, _, outputCards) = parser.parseCards();
        return outputCards.Select(o => (o.Id, o.Output)).ToList();
    }

    public class CardData
    {
        public int Id { get; set; }
        public GateTypeEnum GateType { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsLocked { get; set; }
    }

    public class ConnectionData
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public int TargetInputIndex { get; set; }
    }
}