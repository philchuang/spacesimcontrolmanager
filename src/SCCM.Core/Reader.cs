using System.Collections;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using static SCCM.Core.Extensions;

namespace SCCM.Core;

public class Reader
{
    public string ActionMapsXmlPath { get; private set; }

    private IList<InputDevice> _inputs = new InputDevice[] {};
    public IReadOnlyList<InputDevice> Inputs { get { return (IReadOnlyList<InputDevice>) _inputs; } }

    private IList<Mapping> _mappings = new Mapping[] {};
    public IReadOnlyList<Mapping> Mappings { get { return (IReadOnlyList<Mapping>) _mappings; } }

    public Reader(string path)
    {
        this.ActionMapsXmlPath = path;
    }

    public async Task Read()
    {
        this._inputs = new List<InputDevice>();
        this._mappings = new List<Mapping>();

        using (var fs = new FileStream(this.ActionMapsXmlPath, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

            ReadXDocument(xd);
        }
    }

    private IEnumerable<XElement> GetChildren(XNode node, string childName)
    {
        var xe = node as XElement;
        if (xe == null) return new XElement[] {};

        return xe.DescendantNodes().Where(n => n is XElement xe && xe.Name.LocalName.Equals(childName)).Select(n => (XElement) n);
    }

    private string? GetAttribute(XElement node, string attrName)
    {
        return node?.Attribute(attrName)?.Value;
    }

    private void ReadXDocument(XDocument xd)
    {
        if (xd.Root == null)
        {
            throw new InvalidDataException($"Expecting <ActionMaps>, found nothing!");
        }

        if (!xd.Root.Name.LocalName.Equals("ActionMaps"))
        {
            throw new InvalidDataException($"Expecting <ActionMaps>, found <{xd.Root.Name.LocalName}>!");
        }

        var actionMaps = GetChildren(xd, "ActionMaps").First();
        ReadActionMaps(actionMaps);
    }

    private void ReadActionMaps(XElement actionMaps)
    {
        var actionProfiles = GetChildren(actionMaps, "ActionProfiles");
        foreach (var ap in actionProfiles)
        {
            ReadActionProfiles(ap);
        }
    }

    private void ReadActionProfiles(XElement actionProfiles)
    {
        var optionsNodes = GetChildren(actionProfiles, "options");
        foreach (var o in optionsNodes)
        {
            ReadOptions(o);
        }
        var actionmapNodes = GetChildren(actionProfiles, "actionmap");
        foreach (var am in actionmapNodes)
        {
            ReadActionMap(am);
        }
    }

    private void ReadOptions(XElement option)
    {
        var input = new InputDevice { Type = GetAttribute(option, "type"), Instance = IntTryParseOrDefault(GetAttribute(option, "instance"), -1), Product = GetAttribute(option, "Product") };
        // TODO if joystick check for other child nodes
        this._inputs.Add(input);
    }

    private void ReadActionMap(XElement actionmap)
    {
    }
}
