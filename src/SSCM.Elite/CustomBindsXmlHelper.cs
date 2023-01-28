using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using static SSCM.Core.XmlExtensions;

namespace SSCM.Elite;

public class CustomBindsXmlHelper
{
    public XDocument Xml { get; init; }

    public CustomBindsXmlHelper(XDocument xd)
    {
        this.Xml = xd;
        this.Validate();
    }

    public static async Task<CustomBindsXmlHelper> Load(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find the Elite Dangerous binds file at [{path}]!");
        }

        using (var fs = new FileStream(path, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);
            
            return new CustomBindsXmlHelper(xd);
        }
    }

    private void Validate()
    {
        if (this.Xml.Root == null)
        {
            throw new InvalidDataException($"Expecting <Root>, found nothing!");
        }

        if (!this.Xml.Root.Name.LocalName.Equals("Root"))
        {
            throw new InvalidDataException($"Expecting <Root>, found <{this.Xml.Root.Name.LocalName}>!");
        }
    }

    public async Task Save(string path)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var xw = XmlWriter.Create(fs, new XmlWriterSettings { Async = true, Indent = true }))
        {
            var ct = new CancellationToken();
            await this.Xml.WriteToAsync(xw, ct);
        }
    }

    #region Helpers
    public XElement GetOrCreateMapping(string name)
    {
        var xe = this.Xml.XPathSelectElement($"Root/{name}");
        return xe ?? this.Xml.Root!.GetOrCreate(name);
    }
    #endregion
}