using System.Xml.Linq;
using GroupProject.Services;
using GroupProject.Model.LearnerModel;
using GroupProject.Model.Utilities;
using GroupProject.Model.LogicModel;

namespace GroupProject.ViewModel;

public class LearnerViewModel
{
	private readonly string statePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "State.xml");

	private readonly XmlStateService xmlService;

	public LearnerViewModel()
	{
		xmlService = new XmlStateService(statePath);
	}

    public XmlStateService GetXmlStateService()
    {
        return xmlService;
    }

	public void SaveStateToXml(LogicState state, XmlStateService xmlService)
	{
		xmlService.ClearStateFile();

		List<int> usedIds = xmlService.GetAllIds();
		int _nextID = 1;
		Func<int> getNextAvailableId = () =>
		{
			while (usedIds.Contains(_nextID))
				_nextID++;
			usedIds.Add(_nextID);
			return _nextID++;
		};

		Dictionary<int, int> indexToId = new Dictionary<int, int>();

		for (int index = 0; index < state.Gates.Count; index++)
		{
			string gate = state.Gates[index];
			if (gate.StartsWith("Input"))
			{
				int id = getNextAvailableId();
				indexToId[index] = id;
				xmlService.AddInputCard(id, false, 0, 0);
			}
		}

		for (int index = 0; index < state.Gates.Count; index++)
		{
			string gate = state.Gates[index];
			if (!gate.StartsWith("Input") && !gate.StartsWith("Output"))
			{
				var gateType = gate.Split(' ')[0];
				int id = getNextAvailableId();
				indexToId[index] = id;
				xmlService.AddLogicGateCard(id, gateType, 0, 0, 0, 0);
			}
		}

		for (int index = 0; index < state.Gates.Count; index++)
		{
			string gate = state.Gates[index];
			if (gate.StartsWith("Output"))
			{
				int id = getNextAvailableId();
				indexToId[index] = id;
				xmlService.AddOutputCard(id, 0, 0, 0);
			}
		}

		Dictionary<int, int> targetConnectionCount = new Dictionary<int, int>();
		foreach (var connection in state.Connections)
		{
			int sourceId = indexToId[connection.from];
			int targetId = indexToId[connection.to];
			int portIndex = 1;
			if (targetConnectionCount.ContainsKey(targetId))
			{
				portIndex = 2;
				targetConnectionCount[targetId]++;
			}
			else
			{
				targetConnectionCount[targetId] = 1;
			}
			xmlService.SetCardInput(targetId, portIndex, sourceId);
		}
	}

	public void ReshuffleIds()
	{
		var xmlService = new XmlStateService(statePath);
		var _doc = xmlService.Document;

		var cards = new List<CardInfo>();

		foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
		{
			var oldId = (int)elem.Attribute("id");
			cards.Add(new CardInfo { OldId = oldId, Type = "Input" });
		}

		foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
		{
			var oldId = (int)elem.Attribute("id");
			var info = new CardInfo { OldId = oldId, Type = "LogicGate" };

			int input1 = (int)elem.Attribute("input1");
			int input2 = (int)elem.Attribute("input2");
			if (input1 != 0)
				info.Dependencies.Add(input1);
			if (input2 != 0)
				info.Dependencies.Add(input2);

			cards.Add(info);
		}

		foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
		{
			var oldId = (int)elem.Attribute("id");
			var info = new CardInfo { OldId = oldId, Type = "Output" };

			int input1 = (int)elem.Attribute("input1");
			if (input1 != 0)
				info.Dependencies.Add(input1);
			cards.Add(info);
		}

		var lookup = cards.ToDictionary(c => c.OldId);

		var inDegree = new Dictionary<int, int>();
		foreach (var card in cards) inDegree[card.OldId] = 0;
		foreach (var card in cards)
			foreach (var dep in card.Dependencies)
				inDegree[card.OldId]++;

		var ready = new Queue<CardInfo>(cards.Where(c => inDegree[c.OldId] == 0));
		var sorted = new List<CardInfo>();

		while (ready.Count > 0)
		{
			var card = ready.Dequeue();
			sorted.Add(card);

			foreach (var dependent in cards.Where(c => c.Dependencies.Contains(card.OldId)))
			{
				inDegree[dependent.OldId]--;
				if (inDegree[dependent.OldId] == 0)
					ready.Enqueue(dependent);
			}
		}

		if (sorted.Count != cards.Count)
			throw new Exception("A dependency cycle was detected among the cards. Reshuffling is not possible.");


		var newId = 1;
		var idMapping = new Dictionary<int, int>();
		foreach (var card in sorted)
		{
			card.NewId = newId;
			idMapping[card.OldId] = newId;
			newId++;
		}

		foreach (var elem in _doc.Descendants("InputCards").Elements("ICard"))
		{
			var oldId = (int)elem.Attribute("id");
			if (idMapping.ContainsKey(oldId))
				elem.SetAttributeValue("id", idMapping[oldId]);
		}

		foreach (var elem in _doc.Descendants("LogicGateCards").Elements("LogicGate"))
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

		foreach (var elem in _doc.Descendants("OutputCards").Elements("OCard"))
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

		xmlService.Save();
	}

	public List<CardData> LoadCards()
	{
		var _xmlService = new XmlStateService(statePath);
		var doc = _xmlService.Document;
		var cards = new List<CardData>();

		cards.AddRange(doc.Descendants("InputCards").Elements("ICard").Select(e => new CardData
		{
			Id = XmlHelper.GetAttrInt(e, "id"),
			GateType = GateTypeEnum.Input,
			X = XmlHelper.GetAttrDouble(e, "xPos"),
			Y = XmlHelper.GetAttrDouble(e, "yPos")
		}));

		cards.AddRange(doc.Descendants("OutputCards").Elements("OCard").Select(e => new CardData
		{
			Id = XmlHelper.GetAttrInt(e, "id"),
			GateType = GateTypeEnum.Output,
			X = XmlHelper.GetAttrDouble(e, "xPos"),
			Y = XmlHelper.GetAttrDouble(e, "yPos")
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
		var _xmlService = new XmlStateService(statePath);
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
	
    public class CardData
    {
        public int Id { get; set; }
        public GateTypeEnum GateType { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class ConnectionData
    {
        public int FromId { get; set; }
        public int ToId { get; set; }
        public int TargetInputIndex { get; set; }
    }
}