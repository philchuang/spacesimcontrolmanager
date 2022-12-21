using System.Collections;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using static SCCM.Core.Extensions;

namespace SCCM.Core;

public class DataReader
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ActionMapsXmlPath { get; private set; }

    private MappingData _data = new MappingData();

    public DataReader(string path)
    {
        this.ActionMapsXmlPath = path;
    }

    public async Task<MappingData> Read()
    {
        this._data = new MappingData { ReadTime = DateTime.UtcNow };

        using (var fs = new FileStream(this.ActionMapsXmlPath, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

            ReadXDocument(xd);
        }

        this.StandardOutput($"Read in {this._data.Inputs.Count} input devices.");
        this.StandardOutput($"Read in {this._data.Mappings.Count} mappings.");
        return this._data;
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

        var actionMaps = xd.Root;
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
        var actionProfilesName = GetAttribute(actionProfiles, "profileName");
        this.DebugOutput($"Processing ActionProfiles [{actionProfilesName}]...");
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
        var product = GetAttribute(option, "Product");
        if (string.IsNullOrWhiteSpace(product)) return;

        this.DebugOutput($"Processing options [{product}]...");

        var input = new InputDevice { Type = GetAttribute(option, "type"), Instance = IntTryParseOrDefault(GetAttribute(option, "instance"), -1), Product = product };
        foreach (var prop in option.Descendants())
        {
            input.Settings.Add(
                new InputDeviceSetting
                {
                    Name = prop.Name.LocalName,
                    Properties = prop.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value),
                }
            );
        }
        this._data.Inputs.Add(input);
    }

    private void ReadActionMap(XElement actionmap)
    {
        var actionmapName = GetAttribute(actionmap, "name");
        if (string.IsNullOrWhiteSpace(actionmapName))
        {
            this.WarningOutput($"Found actionmap node without a name: {actionmap}");
            return;
        }

        this.DebugOutput($"Processing actionmap [{actionmapName}]...");
        var actions = GetChildren(actionmap, "action");
        foreach (var a in actions)
        {
            ReadAction(a, actionmapName);
        }
    }

    private void ReadAction(XElement action, string actionmapName)
    {
        var actionName = GetAttribute(action, "name");
        if (string.IsNullOrWhiteSpace(actionName))
        {
            this.WarningOutput($"Found action node without a name: {action}");
            return;
        }
        var rebind = GetChildren(action, "rebind").FirstOrDefault();
        if (rebind == null)
        {
            this.WarningOutput($"Found action node without rebind childnodes: {actionName}");
            return;
        }
        var input = GetAttribute(rebind, "input");
        if (string.IsNullOrWhiteSpace(input))
        {
            this.WarningOutput($"Found rebind node without input value: {actionName}");
            return;
        }
        var multitapStr = GetAttribute(rebind, "multiTap");
        if (!int.TryParse(multitapStr, out var multitap)) multitap = -1;
        this._data.Mappings.Add(new Mapping { ActionMap = actionmapName, Action = actionName, Input = input, MultiTap = multitap != -1 ? multitap : null, Preserve = true });
    }
}
