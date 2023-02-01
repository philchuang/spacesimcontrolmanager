using System.Xml.Linq;
using SSCM.Core;
using static SSCM.StarCitizen.Extensions;

namespace SSCM.StarCitizen;

public class MappingImporter : IMappingImporter<SCMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string GameMappingsPath => this._folders.GameMappingsPath;
    public string GameAttributesPath => this._folders.GameAttributesPath;

    private readonly IPlatform _platform;
    private readonly ISCFolders _folders;

    private SCMappingData _data = new SCMappingData();

    public MappingImporter(IPlatform platform, ISCFolders folders)
    {
        this._platform = platform;
        this._folders = folders;
    }

    public async Task<SCMappingData> Read()
    {
        this._data = new SCMappingData { ReadTime = this._platform.UtcNow };

        await this.ReadMappings();
        await this.ReadAttributes();

        return this._data;
    }
    
    private async Task ReadMappings()
    {
        if (!System.IO.File.Exists(this.GameMappingsPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.GameMappingsPath}]!");
        }

        this.StandardOutput($"Reading [{this.GameMappingsPath}]...");
        using (var fs = new FileStream(this.GameMappingsPath, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

            ReadMappingsDocument(xd);
        }
        this.StandardOutput($"Read in {this._data.Inputs.Count} input devices.");
        this.StandardOutput($"Read in {this._data.Mappings.Count} mappings.");
    }

    private void ReadMappingsDocument(XDocument xd)
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
        var actionProfiles = actionMaps.GetChildren("ActionProfiles").Single(ap => ap.GetAttribute("profileName") == "default");
        ReadActionProfiles(actionProfiles);
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

        var input = new SCInputDevice { Type = option.GetAttribute("type"), Instance = IntTryParseOrDefault(option.GetAttribute("instance"), -1), Preserve = true, Product = product };
        foreach (var prop in option.Elements())
        {
            var setting = new SCInputDeviceSetting
            {
                Name = prop.Name.LocalName,
                Parent = $"{input.Type}-{input.Instance}-{input.Product}", // I think product is probably sufficient, assuming the GUID is unique to the device
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
        var actionName = action.GetAttribute("name");
        if (string.IsNullOrWhiteSpace(actionName))
        {
            this.WarningOutput($"Found action node without a name: {action}");
            return;
        }

        foreach (var rebind in action.GetChildren("rebind"))
        {
            ReadRebinds(rebind, actionmapName, actionName);
        }
    }

    private void ReadRebinds(XElement rebind, string actionmapName, string actionName)
    {
        var preserve = true;

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
            // not invalid, just SC's way of saying it's unbound
            // preserve it since it probably means that the user manually unbound it
        }
        var multitapStr = rebind.GetAttribute("multiTap");
        if (!int.TryParse(multitapStr, out var multitap)) multitap = -1;
        var (inputType, instance) = ActionMapsXmlHelper.GetOptionsTypeAndInstanceForPrefix(input);
        this._data.Mappings.Add(new SCMapping { ActionMap = actionmapName, Action = actionName, Input = input, InputType = inputType, MultiTap = multitap != -1 ? multitap : null, Preserve = preserve });
    }

    private async Task ReadAttributes()
    {
        if (!System.IO.File.Exists(this.GameAttributesPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen attributes file at [{this.GameAttributesPath}]!");
        }

        this.StandardOutput($"Reading [{this.GameAttributesPath}]...");
        using (var fs = new FileStream(this.GameAttributesPath, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

            ReadAttributesDocument(xd);
        }
        this.StandardOutput($"Read in {this._data.Attributes.Count} attributes.");
    }

    private void ReadAttributesDocument(XDocument xd)
    {
        if (xd.Root == null)
        {
            throw new InvalidDataException($"Expecting <Attributes>, found nothing!");
        }

        if (!xd.Root.Name.LocalName.Equals("Attributes"))
        {
            throw new InvalidDataException($"Expecting <Attributes>, found <{xd.Root.Name.LocalName}>!");
        }

        ReadAttributes(xd.Root);
    }

    private void ReadAttributes(XElement root)
    {
        var attrs = root.GetChildren("Attr");
        foreach (var a in attrs)
        {
            ReadAttribute(a);
        }
    }

    private void ReadAttribute(XElement attrElement)
    {
        var name = attrElement.GetAttribute("name");
        var value = attrElement.GetAttribute("value");

        var attr = new SCAttribute {
            Name = name,
            Value = value,
            Preserve = !string.IsNullOrWhiteSpace(value),
        };

        this._data.Attributes.Add(attr);
    }
}
