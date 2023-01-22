using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core;
using static SSCM.Core.XmlExtensions;

namespace SSCM.Elite;

public class MappingImporter : IMappingImporter<EDMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string GameConfigPath { get; private set; }

    private readonly IPlatform _platform;

    private EDMappingData _data = new EDMappingData();

    public MappingImporter(IPlatform platform, string gameConfigPath)
    {
        this._platform = platform;
        this.GameConfigPath = gameConfigPath;
    }

    public async Task<EDMappingData> Read()
    {
        if (!System.IO.File.Exists(this.GameConfigPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.GameConfigPath}]!");
        }

        this._data = new EDMappingData { ReadTime = this._platform.UtcNow };

        this.StandardOutput($"Reading [{this.GameConfigPath}]...");
        using (var fs = new FileStream(this.GameConfigPath, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

            ReadXDocument(xd);
        }

        // this.StandardOutput($"Read in {this._data.Inputs.Count} input devices.");
        // this.StandardOutput($"Read in {this._data.Mappings.Count} mappings.");
        return this._data;
    }

    private void ReadXDocument(XDocument xd)
    {
        if (xd.Root == null)
        {
            throw new InvalidDataException($"Expecting <Root>, found nothing!");
        }

        if (!xd.Root.Name.LocalName.Equals("Root"))
        {
            throw new InvalidDataException($"Expecting <Root>, found <{xd.Root.Name.LocalName}>!");
        }

        var root = xd.Root;
        ReadRootElement(root);
    }

    private static readonly IDictionary<string, string> CUSTOM_BIND_TO_GROUPING_MAP = new Dictionary<string, string> {
        ["^.*Thrust.*$"] = "Flying",
        ["BackwardKey"] = "TBD",
        ["ForwardKey"] = "TBD",
        ["^.*Buggy.*$"] = "Driving",
        ["^Cam.+$"] = "Camera",
        ["^ChargeECM$"] = "Combat",
        ["^CommanderCreator$"] = "Holo-Me",
        ["^CommsPanelFocusOptions$"] = "Flight UI",
        ["^CqcMuteButtonMode$"] = "PTT",
        ["^CycleFireGroup.+"] = "Combat",
        ["^Cycle.+Target.*"] = "Combat Targeting",
        ["^Cycle.+(Page|Panel)$"] = "Flight UI",
        // TBD
    };

    private static readonly Lazy<IList<Tuple<Regex, string>>> GROUPING_REGEXES = new Lazy<IList<Tuple<Regex, string>>>(() => InitGroupingMapRegex().ToList());

    private static IEnumerable<Tuple<Regex, string>> InitGroupingMapRegex()
    {
        foreach (var kvp in CUSTOM_BIND_TO_GROUPING_MAP)
        {
            yield return new Tuple<Regex, string>(new Regex(kvp.Key), kvp.Value);
        }
    }

    private static string? DetermineGroupForMapping(string mappingName)
    {
        foreach (var tuple in GROUPING_REGEXES.Value)
        {
            if (tuple.Item1.IsMatch(mappingName)) return tuple.Item2;
        }

        return null;
    }

    private void ReadRootElement(XElement rootElement)
    {
        foreach (var e in rootElement.Elements())
        {
            if (!e.HasElements)
            {
                this._data.Settings.AddIfNotNull(ReadGenericSetting(e));
            }
            else
            {
                this._data.Mappings.AddIfNotNull(ReadMappingElement(e));
            }
        }
    }

    private EDMappingSetting? ReadGenericSetting(XElement settingElement)
    {
        var name = settingElement.Name.LocalName;
        var value = settingElement.GetAttribute("Value");

        if (string.IsNullOrWhiteSpace(value))
        {
            WarningOutput($"Could not process <{name}>!");
            return null;
        }

        DebugOutput($"Captured setting [{name}].");
        return new EDMappingSetting {
            Name = name,
            Value = value,
            Preserve = true,
        };
    }

    private EDMapping? ReadMappingElement(XElement mappingElement)
    {
        var name = mappingElement.Name.LocalName;

        var group = DetermineGroupForMapping(name);
        if (string.IsNullOrWhiteSpace(group))
        {
            WarningOutput($"Could not determine mapping group for <{name}>!");
            return null;
        }

        DebugOutput($"Processing mapping [{group}-{name}]...");
        var mapping = new EDMapping {
            Group = group,
            Name = name,
        };

        foreach (var childElement in mappingElement.Elements())
        {
            ReadMappingChildren(childElement, mapping);
        }

        return mapping;
    }

    private void ReadMappingChildren(XElement childElement, EDMapping mapping)
    {
        var elementName = childElement.Name.LocalName;

        if (string.Equals("Binding", elementName, StringComparison.OrdinalIgnoreCase) || 
            string.Equals("Primary", elementName, StringComparison.OrdinalIgnoreCase))
        {
            mapping.Primary = ReadBindingElement(childElement);
            if (mapping.Primary != null)
            {
                DebugOutput($"Captured Primary binding [{mapping.Primary.Device}-{mapping.Primary.Key}].");
            }
            return;
        }

        if (string.Equals("Secondary", elementName, StringComparison.OrdinalIgnoreCase))
        {
            mapping.Secondary = ReadBindingElement(childElement);
            if (mapping.Secondary != null)
            {
                DebugOutput($"Captured Secondary binding [{mapping.Secondary.Device}-{mapping.Secondary.Key}].");
            }
            return;
        }

        mapping.Settings.AddIfNotNull(ReadGenericSetting(childElement));
    }

    private EDBinding? ReadBindingElement(XElement bindingElement)
    {
        string? device = null;
        string? key = null;
        foreach (var attr in bindingElement.Attributes())
        {
            if (string.Equals("Device", attr.Name.LocalName, StringComparison.OrdinalIgnoreCase))
            {
                device = attr.Value;
            }
            else if (string.Equals("Key", attr.Name.LocalName, StringComparison.OrdinalIgnoreCase))
            {
                key = attr.Value;
            }
            else
            {
                WarningOutput($"Unhandled attribute [{attr.Name.LocalName}]!");
            }
        }
        
        if (string.IsNullOrWhiteSpace(device))
        {
            WarningOutput($"Could not find @device value!");
            return null;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            WarningOutput($"Could not find @key value!");
            return null;
        }

        return new EDBinding {
            Device = device,
            Key = key,
            Preserve = true,
        };
    }
}
