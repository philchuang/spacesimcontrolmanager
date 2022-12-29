using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using static SCCM.Core.XmlExtensions;

namespace SCCM.Core;

public class MappingExporter
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ActionMapsXmlPath { get; private set; }

    private readonly IPlatform _platform;
    private readonly IFolders _folders;
    
    public MappingExporter(IPlatform platform, IFolders folders, string actionmapsxmlpath)
    {
        this._platform = platform;
        this._folders = folders;
        this.ActionMapsXmlPath = actionmapsxmlpath;
    }

    public string Backup()
    {
        if (!System.IO.File.Exists(this.ActionMapsXmlPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.ActionMapsXmlPath}]!");
        }

        // make backup of actionmaps.xml
        var actionmapsxmlBackup = System.IO.Path.Combine(this._folders.SccmDir, $"actionmaps.xml.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        System.IO.File.Copy(this.ActionMapsXmlPath, actionmapsxmlBackup);
        return actionmapsxmlBackup;
    }

    public string RestoreLatest()
    {
        // find all files matching pattern, sort ordinally
        var backups = System.IO.Directory.GetFiles(this._folders.SccmDir, "actionmaps.xml.*.bak");
        var latest = backups.OrderBy(s => s).LastOrDefault();
        if (latest == null)
        {
            throw new FileNotFoundException($"Could not find any backup files in [{this._folders.SccmDir}]!");
        }

        // copy latest file to actionmaps.xml
        System.IO.File.Copy(latest, this.ActionMapsXmlPath, true);

        return latest;
    }

    public async Task Preview(MappingData data)
    {
        await this.Export(data, false);
    }

    public async Task Update(MappingData data)
    {
        await this.Export(data, true);
    }

    private void Validate(MappingData data)
    {
        // TODO implement

        // if inputs are preserved, they need to preserved in contiguous order (e.g. 1, 1-2, and not 2, 1-3)
        // all preserved mappings need to reference a preserved input
    }

    private async Task Export(MappingData data, bool apply)
    {
        if (!System.IO.File.Exists(this.ActionMapsXmlPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.ActionMapsXmlPath}]!");
        }

        this.Validate(data);

        using (var fs = new FileStream(this.ActionMapsXmlPath, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

            this.SetupXDocument(xd);
        }

        this.ExportInputDevices(data.Inputs);
        this.ExportMappings(data.Mappings);

        if (!apply) return;

        this.StandardOutput($"Saving new actionmaps.xml...");
        using (var fs = new FileStream(this.ActionMapsXmlPath, FileMode.Create))
        using (var xw = XmlWriter.Create(fs, new XmlWriterSettings { Async = true, Indent = true }))
        {
            var ct = new CancellationToken();
            await this._xd.WriteToAsync(xw, ct);
        }
        this.StandardOutput($"Saved, run \"restore\" command to revert.");
    }

    private XDocument? _xd;
    private XElement? _actionProfilesDefaultElement;

    private void SetupXDocument(XDocument xd)
    {
        if (xd.Root == null)
        {
            throw new InvalidDataException($"Expecting <ActionMaps>, found nothing!");
        }

        if (!xd.Root.Name.LocalName.Equals("ActionMaps"))
        {
            throw new InvalidDataException($"Expecting <ActionMaps>, found <{xd.Root.Name.LocalName}>!");
        }

        this._xd = xd;
        this._actionProfilesDefaultElement = this._xd.Root.GetChildren("ActionProfiles").Single(ap => ap.GetAttribute("profileName") == "default");
        if (this._actionProfilesDefaultElement == null)
        {
            throw new InvalidDataException($"Could not find <ActionProfiles> with profileName [default].");
        }
    }

    private static Regex XML_REGEX = new Regex(@"<(\w[\w\d_]+)[^>]*>.+</\1>");

    private static string GetPrefixForOptionsElement(XElement options)
    {
        var type = options.GetAttribute("type");
        var instance = options.GetAttribute("instance");

        var typeAbbv = type switch {
            "joystick" => "js",
            "keyboard" => "kb",
            _ => throw new ArgumentOutOfRangeException(type),
        };

        return $"{typeAbbv}{instance}_";
    }

    private (string, string) GetOptionsTypeAndInstanceForPrefix(string prefix)
    {
        var regex = new Regex(@"^(\w+)(\d+)_.*$");
        var match = regex.Match(prefix);
        var typeAbbv = match.Groups[1].Value;
        var instance = match.Groups[2].Value;
        var type = typeAbbv switch {
            "js" => "joystick",
            "kb" => "keyboard",
            _ => throw new ArgumentOutOfRangeException(typeAbbv),
        };
        return (type, instance);
    }

    private List<XElement> GetAllActionRebindsForOptions(XElement options)
    {
        return this.GetAllActionRebindsForInputPrefix(GetPrefixForOptionsElement(options));
    }

    private List<XElement> GetAllActionRebindsForInputPrefix(string prefix)
    {
        return this._actionProfilesDefaultElement.XPathSelectElements($"//*/rebind[starts-with(@input, '{prefix}')]").ToList();
    }

    private XElement? GetOptionsElementForInput(InputDevice input)
    {
        return this._actionProfilesDefaultElement.XPathSelectElements($"//*/options[@type='{input.Type}' and @instance='{input.Instance}' and @Product='{input.Product}']").SingleOrDefault();
    }

    private XElement? GetElementForInputSetting(InputDevice input, string settingName)
    {
        return this._actionProfilesDefaultElement.XPathSelectElements($"//*/options[@type='{input.Type}' and @instance='{input.Instance}' and @Product='{input.Product}']/{settingName}").SingleOrDefault();
    }

    private XElement? GetActionmapForMapping(Mapping mapping)
    {
        return this._actionProfilesDefaultElement.XPathSelectElements($"//*/actionmap[@name='{mapping.ActionMap}']").SingleOrDefault();
    }

    private XElement? GetActionForMapping(Mapping mapping)
    {
        return this._actionProfilesDefaultElement.XPathSelectElements($"//*/actionmap[@name='{mapping.ActionMap}']/action[@name='{mapping.Action}']").SingleOrDefault();
    }

    private void RestoreInputs(IEnumerable<InputDevice> inputs)
    {
        // only restore joysticks for now
        var preserved = inputs.Where(i => string.Equals("joystick", i.Type, StringComparison.OrdinalIgnoreCase) && i.Preserve).ToList();
        var addInputs = new List<InputDevice>();
        var remapOptionsMap = new Dictionary<string, (InputDevice input, XElement options, IList<XElement> rebinds)>();
        var removeForPrefixes = new List<string>();
        foreach (var exportInput in preserved)
        {
            var targetInputByInstance = this._xd.XPathSelectElements($"//*/options[@type='{exportInput.Type}' and @instance='{exportInput.Instance}']").SingleOrDefault();
            var targetInputByProduct = this._xd.XPathSelectElements($"//*/options[@type='{exportInput.Type}' and @Product='{exportInput.Product}']").SingleOrDefault();

            if (targetInputByProduct != null)
            { // exported input exists
                if (string.Equals(exportInput.Instance.ToString(), targetInputByProduct.GetAttribute("instance"), StringComparison.OrdinalIgnoreCase))
                { // 1-1: exported input matches
                    // do nothing
                    continue;
                }

                // 1-0: exported input has a different instance ID
                // remap bindings
                remapOptionsMap[exportInput.GetMappingPrefix()] = (exportInput, targetInputByProduct, GetAllActionRebindsForOptions(targetInputByProduct));
                // delete bindings that are in the way of the exported input instance
                removeForPrefixes.Add(GetPrefixForOptionsElement(targetInputByProduct));
                continue;
            }
            
            if (targetInputByInstance != null)
            { // 0-1: exported input doesn't exist and something else has that instance ID
                // delete bindings that are in the way of the exported input instance
                removeForPrefixes.Add(GetPrefixForOptionsElement(targetInputByInstance));
            }
            else
            { // 0-0: exported input doesn't exist and nothing else has that instance ID
                addInputs.Add(exportInput);
            }
        }

        // delete via removeInputPrefixes if not in remapInputPrefixes.Keys
        foreach (var prefix in removeForPrefixes.Where(p => !remapOptionsMap.ContainsKey(p)))
        {
            this.StandardOutput($"Removing mappings like [{prefix}]...");
            this.GetAllActionRebindsForInputPrefix(prefix).ForEach(rebind => rebind.Parent.Remove());
            var (type, instance) = GetOptionsTypeAndInstanceForPrefix(prefix);
            this.StandardOutput($"Removing input for {type} {instance}...");
            this._actionProfilesDefaultElement.XPathSelectElement($"options[@type='{type}' and @instance='{instance}']").Remove();
        }

        // remap via remapInputPrefixes
        foreach (var newPrefixAndElements in remapOptionsMap)
        {
            var newPrefix = newPrefixAndElements.Key;
            var (input, options, rebinds) = newPrefixAndElements.Value;
            var oldPrefix = GetPrefixForOptionsElement(options);
            this.StandardOutput($"Rebinding mappings from [{oldPrefix}] to [{newPrefix}]...");
            foreach (var rebind in rebinds)
            {
                rebind.SetAttributeValue("input", rebind.GetAttribute("input").Replace(oldPrefix, newPrefix));
            }
            
            options.SetAttributeValue("instance", input.Instance);
        }

        // add missing inputs
        foreach (var input in addInputs)
        {
            var options = new XElement("options", new XAttribute("type", input.Type), new XAttribute("instance", input.Instance.ToString()), new XAttribute("Product", input.Product));
            this._actionProfilesDefaultElement.Add(options);
        }
    }

    private void ExportInputDevices(IEnumerable<InputDevice> inputs)
    {
        this.RestoreInputs(inputs);

        foreach (var input in inputs)
        {
            foreach (var setting in input.Settings.Where(s => s.Preserve))
            {
                var settingElement = this.GetElementForInputSetting(input, setting.Name);
                if (settingElement == null)
                {
                    var inputElement = this.GetOptionsElementForInput(input);
                    if (inputElement == null)
                    {
                        throw new SccmException($"Could not find <options> element for type [{input.Type}] instance [{input.Instance}] Product [{input.Product}].");
                    }

                    this.StandardOutput($"Creating <{setting.Name}>...");
                    // create setting element
                    settingElement = new XElement(setting.Name);
                    inputElement.Add(settingElement);
                    // this._inputElementMap[$"{input.Type}-{input.Instance}-{input.Product}-{setting.Name}"] = settingElement;
                }

                foreach (var prop in setting.Properties)
                {
                    if (XML_REGEX.IsMatch(prop.Value))
                    {
                        // handle XML property
                        var settingValueElement = settingElement.GetChildren(prop.Key).FirstOrDefault();
                        if (settingValueElement != null)
                        {
                            if (string.Equals(settingValueElement.ToString(), prop.Value)) continue; // already exists and matches
                            settingValueElement.Remove(); // already exists and needs to be overwritten
                        }
                        
                        this.StandardOutput($"Updating {input.Product}/<{setting.Name}>/{prop.Value}...");
                        settingValueElement = XElement.Parse(prop.Value);
                        settingElement.Add(settingValueElement);
                    }
                    else if (!string.Equals(settingElement.GetAttribute(prop.Key), prop.Value))
                    {
                        // handle attribute property
                        this.StandardOutput($"Updating {input.Product}/<{setting.Name}>@{prop.Key} to {prop.Value}...");
                        settingElement.SetAttributeValue(prop.Key, prop.Value);                    
                    }
                }
            }
        }
    }

    private void ExportMappings(IEnumerable<Mapping> mappings)
    {
        foreach (var mapping in mappings.Where(m => m.Preserve))
        {
            var actionElement = this.GetActionForMapping(mapping);
            if (actionElement == null)
            {
                var actionmapElement = this.GetActionmapForMapping(mapping);
                if (actionmapElement == null)
                {
                    this.StandardOutput($"Creating <actionmap name=\"{mapping.ActionMap}\">...");
                    // create <actionmap>
                    actionmapElement = new XElement("actionmap");
                    actionmapElement.SetAttributeValue("name", mapping.ActionMap);
                    this._actionProfilesDefaultElement.Add(actionmapElement);
                    // this._actionElementMap[$"{mapping.ActionMap}-"] = actionmapElement;
                }

                this.StandardOutput($"Creating <action name=\"{mapping.Action}\">...");
                // create <action>
                actionElement = new XElement("action");
                actionElement.SetAttributeValue("name", mapping.Action);
                actionmapElement.Add(actionElement);
                // this._actionElementMap[$"{mapping.ActionMap}-{mapping.Action}"] = actionmapElement;
            }

            var rebindElement = actionElement.GetChildren("rebind").SingleOrDefault();
            if (rebindElement == null)
            {
                this.StandardOutput($"Creating <rebind input=\"{mapping.Input}\" />...");
                rebindElement = new XElement("rebind");
                rebindElement.SetAttributeValue("input", mapping.Input);
                actionElement.Add(rebindElement);
            }
            else
            {
                if (!string.Equals(rebindElement.GetAttribute("input"), mapping.Input))
                {
                    this.StandardOutput($"Updating {mapping.ActionMap}-{mapping.Action} to {mapping.Input}...");
                    rebindElement.SetAttributeValue("input", mapping.Input);
                }
            }
        }
    }
}