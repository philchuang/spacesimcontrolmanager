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
    private Dictionary<string, EDMappingSetting> _settingsMap = new Dictionary<string, EDMappingSetting>();
    private Dictionary<string, EDMapping> _mappingsMap = new Dictionary<string, EDMapping>();
    private readonly Lazy<EDMappingConfig> _lazyConfig;
    private EDMappingConfig Config => this._lazyConfig.Value;
    
    public MappingImporter(IPlatform platform, string gameConfigPath)
    {
        this._platform = platform;
        this.GameConfigPath = gameConfigPath;
        this._lazyConfig = new Lazy<EDMappingConfig>(() => EDMappingConfig.Load(Path.Combine(this._platform.WorkingDir, "EDMappingConfig.yml")));
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

        var bindingsCount = 0;
        this._data.Mappings = this._data.Mappings.Where(m =>
        {
            if (m.Primary != null) bindingsCount++;
            if (m.Secondary != null) bindingsCount++;
            // // remove unbound mappings
            // return m.Primary != null || m.Secondary != null;
            return true;
        }).ToList();

        // sort
        this._data.Mappings = this._data.Mappings.OrderBy(m => m.Group).ThenBy(m => m.Name).ToList();
        this._data.Settings = this._data.Settings.OrderBy(s => s.Group).ThenBy(s => s.Name).ToList();

        this.StandardOutput($"Captured {this._data.Mappings.Count} mappings with {bindingsCount} bindings.");
        this.StandardOutput($"Captured {this._data.Settings.Count} settings.");

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

    private void ReadRootElement(XElement rootElement)
    {
        foreach (var e in rootElement.Elements())
        {
            if (this.Config.IsIgnored(e.Name.LocalName))
            {
                DebugOutput($"Skipped [{e.Name.LocalName}].");
                continue;
            }
            
            if (!e.HasElements)
            {
                var setting = ReadGlobalSetting(e);
                if (setting == null)
                    continue;

                if (this._settingsMap.ContainsKey(setting.Id))
                {
                    WarningOutput($"Already captured setting [{setting.Id}], skipping!");
                    continue;
                }

                this._data.Settings.Add(setting);
                this._settingsMap[setting.Id] = setting;
            }
            else
            {
                var mapping = ReadMappingElement(e);
                if (mapping == null)
                    continue;

                if (this._mappingsMap.ContainsKey(mapping.Id))
                {
                    WarningOutput($"Already captured mapping [{mapping.Id}], skipping!");
                    continue;
                }

                this._data.Mappings.Add(mapping);
                this._mappingsMap[mapping.Id] = mapping;
            }
        }
    }

    private EDMappingSetting? ReadGlobalSetting(XElement settingElement)
    {
        var setting = ReadGenericSetting(settingElement, null);
        if (setting == null) return null;

        if (string.Equals("TBD", setting.Group, StringComparison.OrdinalIgnoreCase))
            WarningOutput($"Could not determine mapping group for <{setting.Name}>!");

        return setting;
    }

    private EDMappingSetting? ReadGenericSetting(XElement settingElement, string? group)
    {
        var name = settingElement.Name.LocalName;
        var groups = this.Config.GetGroupsForMapping(name);
        if (groups.Count > 1)
        {
            WarningOutput($"Found multiple group matches for [{name}]: [{string.Join(",", groups)}]");
        }
        group = group ?? groups.First();

        if (!settingElement.Attributes().Any(a => string.Equals("Value", a.Name.LocalName)))
        {
            WarningOutput($"Could not process <{name}>!");
            return null;
        }
        
        var value = settingElement.GetAttribute("Value");
        if (string.IsNullOrWhiteSpace(value))
        {
            DebugOutput($"Setting <{name}> has empty value.");
        }

        DebugOutput($"Captured setting [{group}-{name}] = [{value}].");
        return new EDMappingSetting {
            Group = group,
            Name = name,
            Value = value,
            Preserve = !string.Equals(EDMappingConfig.UNKNOWN_GROUP, group, StringComparison.OrdinalIgnoreCase),
        };
    }

    private EDMapping? ReadMappingElement(XElement mappingElement)
    {
        var name = mappingElement.Name.LocalName;

        var groups = this.Config.GetGroupsForMapping(name);
        if (groups.Count > 1)
        {
            WarningOutput($"Found multiple group matches for [{name}]: [{string.Join(",", groups)}]");
        }
        var group = groups.First();

        if (string.Equals("TBD", group, StringComparison.OrdinalIgnoreCase))
            WarningOutput($"Could not determine mapping group for <{name}>!");

        DebugOutput($"Processing mapping [{group}-{name}]...");
        var mapping = new EDMapping {
            Group = group,
            Name = name,
        };

        foreach (var childElement in mappingElement.Elements())
        {
            ReadMappingChildren(childElement, mapping);
        }

        // sort settings
        if (mapping.Settings.Any())
            mapping.Settings = mapping.Settings.OrderBy(s => s.Name).ToList();

        // unpreserve settings if no bindings
        if ((mapping.Primary == null || mapping.Primary.IsUnbound) && 
            (mapping.Secondary == null || mapping.Secondary.IsUnbound) && 
            mapping.Settings.Any())
        {
            foreach (var s in mapping.Settings)
            {
                s.Preserve = false;
            }
        }

        return mapping;
    }

    private void ReadMappingChildren(XElement childElement, EDMapping mapping)
    {
        var elementName = childElement.Name.LocalName;

        if (string.Equals("Binding", elementName, StringComparison.OrdinalIgnoreCase) || 
            string.Equals("Primary", elementName, StringComparison.OrdinalIgnoreCase))
        {
            mapping.Primary = ReadBindingElement(childElement, $"{mapping.Name}-{elementName}");
            if (mapping.Primary != null)
            {
                DebugOutput($"Captured Primary binding [{mapping.Primary.Key.Id}].");
            }
            return;
        }

        if (string.Equals("Secondary", elementName, StringComparison.OrdinalIgnoreCase))
        {
            mapping.Secondary = ReadBindingElement(childElement, $"{mapping.Name}-{elementName}");
            if (mapping.Secondary != null)
            {
                DebugOutput($"Captured Secondary binding [{mapping.Secondary.Key.Id}].");
            }
            return;
        }

        mapping.Settings.AddIfNotNull(ReadGenericSetting(childElement, mapping.Id));
    }

    private EDBinding? ReadBindingElement(XElement bindingElement, string mappingName)
    {
        var key = ReadBindingKeyElement(bindingElement, mappingName);
        if (key == null) return null;

        var modifiers = bindingElement.Elements()
            .Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase))
            .Select(e => ReadBindingKeyElement(e, mappingName))
            .Where(k => k != null)
            .ToList();

        return new EDBinding {
            Key = key,
            Modifiers = modifiers!,
            Preserve = !string.Equals("{NoDevice}", key.Device, StringComparison.OrdinalIgnoreCase),
        };
    }

    private EDBindingKey? ReadBindingKeyElement(XElement bindingKeyElement, string mappingName)
    {
        string? device = null;
        string? key = null;
        foreach (var attr in bindingKeyElement.Attributes())
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
        }

        if (string.Equals("{NoDevice}", device, StringComparison.OrdinalIgnoreCase))
        {
            DebugOutput($"Empty binding for [{mappingName}].");
        }
        else if (string.IsNullOrWhiteSpace(key))
        {
            WarningOutput($"Could not find @key value!");
        }

        return new EDBindingKey {
            Device = device ?? string.Empty,
            Key = key ?? string.Empty,
        };
    }
}
