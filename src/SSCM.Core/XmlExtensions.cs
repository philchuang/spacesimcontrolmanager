using System.Xml.Linq;

namespace SSCM.Core;

public static class XmlExtensions
{
    public static IEnumerable<XElement> GetChildren(this XElement self, string childName)
    {
        if (self == null) return new XElement[] {};

        return self.Elements().Where(n => n.Name.LocalName.Equals(childName));
    }

    public static XElement GetOrCreate(this XElement self, string childName)
    {
        var xe = self.Elements().Where(e => string.Equals(childName, e.Name.LocalName)).FirstOrDefault();
        if (xe != null) return xe;
 
        xe = new XElement(childName);
        self.Add(xe);
        return xe;
    }

    public static string GetAttribute(this XElement self, string attrName)
    {
        return self?.Attributes().FirstOrDefault(a => a.Name.LocalName.Equals(attrName))?.Value ?? string.Empty;
    }

}