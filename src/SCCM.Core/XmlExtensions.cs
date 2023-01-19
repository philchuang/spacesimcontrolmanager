using System.Xml.Linq;

namespace SCCM.Core;

public static class XmlExtensions
{
    public static IEnumerable<XElement> GetChildren(this XElement self, string childName)
    {
        if (self == null) return new XElement[] {};

        return self.Elements().Where(n => n.Name.LocalName.Equals(childName));
    }

    public static string? GetAttribute(this XElement self, string attrName)
    {
        return self?.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(attrName))?.Value;
    }

}