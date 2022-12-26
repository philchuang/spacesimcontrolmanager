using System.Collections;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using static SCCM.Core.Extensions;
using static SCCM.Core.XmlExtensions;

namespace SCCM.Core;

public class MappingImporter
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ActionMapsXmlPath { get; private set; }

    private readonly IPlatform _platform;

    private MappingData _data = new MappingData();

    public MappingImporter(IPlatform platform, string actionmapsxmlpath)
    {
        this._platform = platform;
        this.ActionMapsXmlPath = actionmapsxmlpath;
    }

    public async Task<MappingData> Read()
    {
        if (!System.IO.File.Exists(this.ActionMapsXmlPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.ActionMapsXmlPath}]!");
        }

        this._data = new MappingData { ReadTime = this._platform.UtcNow };

        this.StandardOutput($"Reading [{this.ActionMapsXmlPath}]...");
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
        var actionProfiles = actionMaps.GetChildren("ActionProfiles");
        foreach (var ap in actionProfiles)
        {
            ReadActionProfiles(ap);
        }
    }

    private void ReadActionProfiles(XElement actionProfiles)
    {
        var actionProfilesName = actionProfiles.GetAttribute("profileName");
        this.DebugOutput($"Processing ActionProfiles [{actionProfilesName}]...");
        var optionsNodes = actionProfiles.GetChildren("options");
        foreach (var o in optionsNodes)
        {
            ReadOptions(o);
        }
        var actionmapNodes = actionProfiles.GetChildren("actionmap");
        foreach (var am in actionmapNodes)
        {
            ReadActionMap(am);
        }
    }

    private void ReadOptions(XElement option)
    {
        var product = option.GetAttribute("Product");
        if (string.IsNullOrWhiteSpace(product)) return;

        this.DebugOutput($"Processing options [{product}]...");

        var input = new InputDevice { Type = option.GetAttribute("type"), Instance = IntTryParseOrDefault(option.GetAttribute("instance"), -1), Preserve = true, Product = product };
        foreach (var prop in option.Elements())
        {
            var setting = new InputDeviceSetting
            {
                Name = prop.Name.LocalName,
                Preserve = true,
                Properties = prop.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value),
            };
            if (prop.Elements().Any())
            {
                this.DebugOutput($"Input {product} has complex property {setting.Name}!");
                foreach (var e in prop.Elements())
                {
                    var propName = e.Name.LocalName;
                    var propValue = e.ToString(SaveOptions.DisableFormatting);
                    setting.Properties[propName] = propValue;
                }
            }
            input.Settings.Add(setting);
        }
        this._data.Inputs.Add(input);
    }

    private void ReadActionMap(XElement actionmap)
    {
        var actionmapName = actionmap.GetAttribute("name");
        if (string.IsNullOrWhiteSpace(actionmapName))
        {
            this.WarningOutput($"Found actionmap node without a name: {actionmap}");
            return;
        }

        this.DebugOutput($"Processing actionmap [{actionmapName}]...");
        var actions = actionmap.GetChildren("action");
        foreach (var a in actions)
        {
            ReadAction(a, actionmapName);
        }
    }

    private void ReadAction(XElement action, string actionmapName)
    {
        var preserve = true;
        var actionName = action.GetAttribute("name");
        if (string.IsNullOrWhiteSpace(actionName))
        {
            this.WarningOutput($"Found action node without a name: {action}");
            return;
        }
        var rebind = action.GetChildren("rebind").FirstOrDefault();
        if (rebind == null)
        {
            this.WarningOutput($"Found action node without rebind childnodes: {actionName}");
            return;
        }
        var input = rebind.GetAttribute("input");
        if (string.IsNullOrWhiteSpace(input))
        {
            this.WarningOutput($"Found rebind node without input value: {actionName}");
            return;
        }
        if (string.IsNullOrWhiteSpace(input.Split("_").LastOrDefault()))
        {
            this.WarningOutput($"Found rebind node with invalid input value: {actionName}, {input}");
            preserve = false;
        }
        var multitapStr = rebind.GetAttribute("multiTap");
        if (!int.TryParse(multitapStr, out var multitap)) multitap = -1;
        this._data.Mappings.Add(new Mapping { ActionMap = actionmapName, Action = actionName, Input = input, MultiTap = multitap != -1 ? multitap : null, Preserve = preserve });
    }
}
