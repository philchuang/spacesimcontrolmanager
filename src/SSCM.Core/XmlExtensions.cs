using System.Xml;
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

    public static async Task<XDocument> LoadAsync(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find file [{path}]!");
        }

        using (var fs = new FileStream(path, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);
            
            return xd;
        }
    }
}
