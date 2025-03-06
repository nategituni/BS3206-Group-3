using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public class LogicGateValidator
{
    private readonly string xmlFilePath;
    private FileSystemWatcher watcher;

    public LogicGateValidator(string path)
    {
        xmlFilePath = path;
        WatchFile();
    }

    private void WatchFile()
    {
        watcher = new FileSystemWatcher(Path.GetDirectoryName(xmlFilePath), Path.GetFileName(xmlFilePath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        watcher.Changed += OnXmlChanged;
        watcher.EnableRaisingEvents = true;
    }

    private void OnXmlChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            Console.WriteLine("XML file updated. Validating...");

            XDocument doc = XDocument.Load(xmlFilePath);

            // Extract Prompt Constraints
            var allowedGates = doc.Descendants("AllowedGates").FirstOrDefault()?.Value.Split(',').Select(g => g.Trim()).ToList();
            var disallowedGates = doc.Descendants("DisallowedGates").FirstOrDefault()?.Value.Split(',').Select(g => g.Trim()).ToList();

            // Extract Expected & User Output
            var expectedOutputs = doc.Descendants("ExpectedOutputs").Elements("Output").Select(x => x.Value).ToList();
            var userOutputs = doc.Descendants("UserSolution").Elements("Outputs").Elements("Output").Select(x => x.Value).ToList();
            var usedGates = doc.Descendants("UserSolution").Elements("UsedGates").FirstOrDefault()?.Value.Split(',').Select(g => g.Trim()).ToList();

            // Check if the user output matches the expected output
            if (!expectedOutputs.SequenceEqual(userOutputs))
            {
                Console.WriteLine("Validation Failed: User's output does not match the expected output.");
                return;
            }

            // Ensure only allowed gates are used
            if (usedGates.Any(gate => !allowedGates.Contains(gate)))
            {
                Console.WriteLine("Validation Failed: User used a gate that is not allowed.");
                return;
            }

            // Ensure no disallowed gates are used
            if (usedGates.Any(gate => disallowedGates.Contains(gate)))
            {
                Console.WriteLine("Validation Failed: User used a disallowed gate.");
                return;
            }

            Console.WriteLine("Validation Passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading XML: {ex.Message}");
        }
    }
}
