using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GroupProject.Model.LogicModel;
using Microsoft.Maui.Storage;

namespace GroupProject.Model.LogicModel
{
    class Validator
    {
        public static void RunValidation(string xmlPath, FileWatcher watcher)
        {
            XDocument doc = XDocument.Load(xmlPath);

            var allCards = new Dictionary<int, IOutputProvider>();

            // --- INPUT CARDS ---
            foreach (var card in doc.Descendants("ICard"))
            {
                int id = int.Parse(card.Attribute("id")!.Value);
                bool value = bool.Parse(card.Attribute("value")!.Value);

                var ioCard = new IOCard { Id = id };
                ioCard.SetValue(value);

                allCards[id] = ioCard;
            }

            // --- LOGIC GATE CARDS ---
            var logicGateElements = doc.Descendants("LogicGate").ToList();
            var logicGates = new List<LogicGateCard>();

            foreach (var gate in logicGateElements)
            {
                int id = int.Parse(gate.Attribute("id")!.Value);
                string gateType = gate.Attribute("gateType")!.Value;
                int input1Id = int.Parse(gate.Attribute("input1")!.Value);
                int input2Id = int.Parse(gate.Attribute("input2")!.Value);

                var gateCard = new LogicGateCard(gateType) { Id = id };
                logicGates.Add(gateCard);
                allCards[id] = gateCard;
            }

            // --- Wire Logic Gate Inputs ---
            foreach (var gateElement in logicGateElements)
            {
                int id = int.Parse(gateElement.Attribute("id")!.Value);
                var gateCard = (LogicGateCard)allCards[id];

                int input1Id = int.Parse(gateElement.Attribute("input1")!.Value);
                int input2Id = int.Parse(gateElement.Attribute("input2")!.Value);

                gateCard.Input1Card = allCards[input1Id];
                gateCard.Input2Card = allCards[input2Id];
            }

            // --- OUTPUT CARDS ---
            var outputCards = doc.Descendants("OCard")
                .ToDictionary(
                    e => int.Parse(e.Attribute("id")!.Value),
                    e =>
                    {
                        int inputId = int.Parse(e.Attribute("input1")!.Value);
                        return allCards[inputId];
                    });

            // --- VALIDATE: Unused LogicGate outputs ---
            var gateIds = logicGates.Select(g => g.Id).ToHashSet();

            var referencedIds = logicGates
                .SelectMany(g => new[] {
                    ((Card)g.Input1Card).Id,
                    ((Card)g.Input2Card).Id
                })
                .Concat(outputCards.Values.Select(c => ((Card)c).Id))
                .ToHashSet();

            var unusedGates = gateIds.Except(referencedIds);
            if (unusedGates.Any())
            {
                Console.WriteLine("Unused LogicGate outputs: " + string.Join(", ", unusedGates));
            }
            else
            {
                Console.WriteLine("All LogicGate outputs are used.");
            }

            // --- CALCULATE LOGIC GATES OUTPUTS ---
            foreach (var gate in logicGates)
            {
                gate.CalculateOutput();
            }

            // --- FINAL OUTPUTS ---
            Console.WriteLine("Final OutputCard Results:");
            foreach (var (id, provider) in outputCards)
            {
                Console.WriteLine($"OCard {id}: {provider.Output}");
            }

            // --- Construct MichaelResources Output ---
            var michaelResources = new XElement("MichaelResources");

            // --- Connectivity Check (including input="0" as invalid) ---
            bool allConnected = true;

            // Logic gates must have valid inputs
            foreach (var gateElement in logicGateElements)
            {
                int input1Id = int.Parse(gateElement.Attribute("input1")!.Value);
                int input2Id = int.Parse(gateElement.Attribute("input2")!.Value);

                if (input1Id == 0 || input2Id == 0)
                {
                    allConnected = false;
                    break;
                }
            }

            // Output cards must also not be wired to "0"
            if (allConnected)
            {
                foreach (var outputCard in doc.Descendants("OCard"))
                {
                    var inputAttr = outputCard.Attribute("input1");
                    if (inputAttr == null || int.Parse(inputAttr.Value) == 0)
                    {
                        allConnected = false;
                        break;
                    }
                }
            }

            // --- OCard Validations ---
            foreach (var oCardElement in doc.Descendants("OCard"))
            {
                int id = int.Parse(oCardElement.Attribute("id")!.Value);
                int inputId = int.Parse(oCardElement.Attribute("input1")!.Value);
                bool actualValue = allCards[inputId].Output;

                var expectedAttr = oCardElement.Attribute("expectedOutput");

                XElement validityElement;
                if (expectedAttr != null)
                {
                    bool expectedValue = bool.Parse(expectedAttr.Value);
                    bool match = expectedValue == actualValue;

                    validityElement = new XElement("OCardValidity",
                        new XAttribute("id", id),
                        new XAttribute("outputValidated", match.ToString().ToLower())
                    );
                }
                else
                {
                    validityElement = new XElement("OCardValidity",
                        new XAttribute("id", id),
                        new XAttribute("outputValidated", "not_provided")
                    );
                }

                michaelResources.Add(validityElement);
            }

            // --- Add AllConnected status ---
            michaelResources.Add(new XElement("AllConnected",
                new XAttribute("connected", allConnected.ToString().ToLower())
            ));

            // --- Replace MichaelResources in document ---
            doc.Descendants("MichaelResources").Remove(); 
            doc.Root?.Add(michaelResources);

            // --- Save XML ---
            doc.Save(xmlPath);
            watcher.MarkFileAsWritten(xmlPath);

            Console.WriteLine($"Modified XML saved to original file: {xmlPath}");
        }
    }
}
