namespace GroupProject.Services;

using System.Xml.Linq;

public class XmlStateService
{
    private XDocument _doc;

	public XDocument Document => _doc;
    private readonly string _filePath;

    // Constructor: Accepts a file path to load or create the XML document.
    public XmlStateService(string filePath)
    {
        _filePath = filePath;
        if (File.Exists(filePath))
        {
            _doc = XDocument.Load(filePath);
        }
        else
        {
            // Create a new document with the required containers.
            _doc = new XDocument(
                new XElement("State",
                    new XElement("InputCards"),
                    new XElement("LogicGateCards"),
                    new XElement("OutputCards")
                )
            );
            Save();
        }
    }

    // Save the current document back to the file.
    public void Save()
    {
        _doc.Save(_filePath);
    }

	// Gets all Id's
	public List<int> GetAllIds()
	{
		return _doc.Descendants()
			.Where(x => x.Attribute("id") != null)
			.Select(x => (int)x.Attribute("id"))
			.ToList();
	}

	public List<int> GetAllIdsThatAreUsedAsInputs()
	{
		return _doc.Descendants()
			.Where(x => x.Attribute("input1") != null || x.Attribute("input2") != null) // Look for either attribute
			.SelectMany(x => new List<int> 
			{ 
				x.Attribute("input1") != null ? (int)x.Attribute("input1") : default, 
				x.Attribute("input2") != null ? (int)x.Attribute("input2") : default 
			})
			.Where(id => id != default) // Remove any default values
			.Distinct() // Ensure unique IDs
			.ToList();
	}

    // Adds an input card element
    public void AddInputCard(int id, bool value, double xPos, double yPos)
    {
        var inputCardsContainer = _doc.Descendants("InputCards").FirstOrDefault();

        if (inputCardsContainer != null)
        {
            inputCardsContainer.Add(new XElement("ICard",
                new XAttribute("id", id),
                new XAttribute("value", value.ToString().ToLower()),
				new XAttribute("xPos", xPos.ToString().ToLower()),
				new XAttribute("yPos", yPos.ToString().ToLower())));
            Save();
        }
    }

	// Deletes an input card element (ICard) with the specified id
	public void DeleteInputCard(int id)
	{
		var inputCardsContainer = _doc.Descendants("InputCards").FirstOrDefault();
		if (inputCardsContainer != null)
		{
			var cardElement = inputCardsContainer
				.Elements("ICard")
				.FirstOrDefault(x => (int)x.Attribute("id") == id);
			if (cardElement != null)
			{
				cardElement.Remove();
				Save();
			}
		}
	}

    // Adds a logic gate card element with two inputs.
    public void AddLogicGateCard(int id, string gateType, int input1Id, int input2Id, double xPos, double yPos)
    {
        var logicGateCardsContainer = _doc.Descendants("LogicGateCards").FirstOrDefault();

        if (logicGateCardsContainer != null)
        {
            logicGateCardsContainer.Add(new XElement("LogicGate",
                new XAttribute("id", id),
                new XAttribute("gateType", gateType),
                new XAttribute("input1", input1Id),
                new XAttribute("input2", input2Id),
				new XAttribute("xPos", xPos.ToString().ToLower()),
				new XAttribute("yPos", yPos.ToString().ToLower())));
            Save();
        }
    }

	// Deletes a logic gate card element (LogicGate) with the specified id
	public void DeleteLogicGateCard(int id)
	{
		var logicGateCardsContainer = _doc.Descendants("LogicGateCards").FirstOrDefault();
		if (logicGateCardsContainer != null)
		{
			var gateElement = logicGateCardsContainer
				.Elements("LogicGate")
				.FirstOrDefault(x => (int)x.Attribute("id") == id);
			if (gateElement != null)
			{
				gateElement.Remove();
				Save();
			}
		}
	}

    // Adds an output card element which references one input.
    public void AddOutputCard(int id, int input1Id, double xPos, double yPos)
    {
        var outputCardsContainer = _doc.Descendants("OutputCards").FirstOrDefault();

        if (outputCardsContainer != null)
        {
            outputCardsContainer.Add(new XElement("OCard",
                new XAttribute("id", id),
                new XAttribute("input1", input1Id),
				new XAttribute("xPos", xPos.ToString().ToLower()),
				new XAttribute("yPos", yPos.ToString().ToLower())));
            Save();
        }
    }

	// Deletes an output card element (OCard) with the specified id
	public void DeleteOutputCard(int id)
	{
		var outputCardsContainer = _doc.Descendants("OutputCards").FirstOrDefault();
		if (outputCardsContainer != null)
		{
			var outputElement = outputCardsContainer
				.Elements("OCard")
				.FirstOrDefault(x => (int)x.Attribute("id") == id);
			if (outputElement != null)
			{
				outputElement.Remove();
				Save();
			}
		}
	}

	// updates card input
	public void UpdateCardInput(int targetCardId, int portIndex, int senderCardId)
	{
		// First, try to locate the card in the LogicGateCards collection.
		var logicGateElement = _doc.Descendants("LogicGateCards")
			.Elements("LogicGate")
			.FirstOrDefault(x => (int)x.Attribute("id") == targetCardId);

		if (logicGateElement != null)
		{
			// For a logic gate, update input1 or input2 depending on the portIndex.
			string attributeName = portIndex == 1 ? "input1" : "input2";
			logicGateElement.SetAttributeValue(attributeName, senderCardId);
			Save();
			return;
		}

		// If not found in LogicGateCards, perhaps it is an OutputCard.
		var outputElement = _doc.Descendants("OutputCards")
			.Elements("OCard")
			.FirstOrDefault(x => (int)x.Attribute("id") == targetCardId);
		
		if (outputElement != null)
		{
			// Output cards in our example only have a single input attribute (input1).
			outputElement.SetAttributeValue("input1", senderCardId);
			Save();
			return;
		}
	}

	public void UpdateInputCardValue(int id, bool newValue)
	{
		var inputCard = _doc.Descendants("InputCards")
			.Elements("ICard")
			.FirstOrDefault(x => (int)x.Attribute("id") == id);
		
		if (inputCard != null)
		{
			inputCard.SetAttributeValue("value", newValue.ToString().ToLower());
			Save();
		}
	}


	public void UpdateCardPosition(int id, double xPos, double yPos)
	{
		// Search for the card element in all possible card groups.
		// Change the element names if necessary to match your XML.
		var cardElement = _doc.Descendants("ICard")
							.Concat(_doc.Descendants("OCard"))
							.Concat(_doc.Descendants("LogicGate"))
							.FirstOrDefault(x => (int)x.Attribute("id") == id);

		if (cardElement != null)
		{
			cardElement.SetAttributeValue("xPos", xPos.ToString());
			cardElement.SetAttributeValue("yPos", yPos.ToString());
			Save();
		}
	}


	    // Clear the state file.
	public void ClearStateFile()
	{
		_doc = new XDocument(
			new XElement("State",
				new XElement("InputCards"),
				new XElement("LogicGateCards"),
				new XElement("OutputCards")
			)
		);
		Save();
	}

	public XDocument ReturnDoc()
	{
		return _doc;
	}

	public void PrintStateFile()
	{
		Console.WriteLine(_doc.ToString());
	}
}