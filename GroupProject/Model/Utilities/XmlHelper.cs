using System.Xml.Linq;

namespace GroupProject.Model.Utilities;

public static class XmlHelper
{
    public static int GetAttrInt(XElement element, string attrName, int defaultValue = 0)
        => int.TryParse(element.Attribute(attrName)?.Value, out var val) ? val : defaultValue;

    public static double GetAttrDouble(XElement element, string attrName, double defaultValue = 0.0)
        => double.TryParse(element.Attribute(attrName)?.Value, out var val) ? val : defaultValue;

    public static string GetAttrString(XElement element, string attrName, string defaultValue = "")
        => element.Attribute(attrName)?.Value ?? defaultValue;
    
    public static bool GetAttrBool(XElement element, string attrName)
    {
        var attr = element.Attribute(attrName);
        return attr != null && bool.TryParse(attr.Value, out var result) && result;
    }

}