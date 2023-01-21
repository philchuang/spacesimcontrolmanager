using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core;
using static SSCM.StarCitizen.Extensions;

namespace SSCM.StarCitizen;

public class MappingExporter : IMappingExporter<MappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string GameConfigPath { get; private set; }

    private static Regex XML_REGEX = new Regex(@"<(\w[\w\d_]+)[^>]*>.+</\1>");

    private readonly IPlatform _platform;
    private readonly ISCFolders _folders;
    private ActionMapsXmlHelper? _xml;
    
    public MappingExporter(IPlatform platform, ISCFolders folders, string actionmapsxmlpath)
    {
        this._platform = platform;
        this._folders = folders;
        this.GameConfigPath = actionmapsxmlpath;
    }

    public string Backup()
    {
        if (!System.IO.File.Exists(this.GameConfigPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.GameConfigPath}]!");
        }

        // make backup of actionmaps.xml
        var actionmapsxmlBackup = System.IO.Path.Combine(this._folders.ScDataDir, $"actionmaps.xml.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        System.IO.File.Copy(this.GameConfigPath, actionmapsxmlBackup);
        return actionmapsxmlBackup;
    }

    public string RestoreLatest()
    {
        // find all files matching pattern, sort ordinally
        var backups = System.IO.Directory.GetFiles(this._folders.ScDataDir, "actionmaps.xml.*.bak");
        var latest = backups.OrderBy(s => s).LastOrDefault();
        if (latest == null)
        {
            throw new FileNotFoundException($"Could not find any backup files in [{this._folders.ScDataDir}]!");
        }

        // copy latest file to actionmaps.xml
        System.IO.File.Copy(latest, this.GameConfigPath, true);

        return latest;
    }

    public async Task<bool> Preview(MappingData source)
    {
        return await this.Export(source, false);
    }

    public async Task<bool> Update(MappingData source)
    {
        return await this.Export(source, true);
    }

    private void Validate(MappingData source)
    {
        var inputPrefixes = source.Inputs.Select(i => i.GetInputPrefix()).ToHashSet();
        foreach (var type in source.Inputs.Select(i => i.Type).Distinct())
        {
            var preservedInputs = source.Inputs.Where(i => i.Type == type && i.Preserve).OrderBy(i => i.Instance).ToList();
            
            // check for valid instance IDs
            var defaultInstance = preservedInputs.Where(i => i.Instance <= 0).FirstOrDefault();
            if (defaultInstance != null)
            {
                throw new SscmException($"Input {defaultInstance.Id} has an invalid Instance value.");
            }
            
            // check input instance IDs are contiguous
            if (preservedInputs.Count > 1)
            {
                var first = preservedInputs.First().Instance;
                for (var i = 0; i < preservedInputs.Count; i++)
                {
                    if (preservedInputs[i].Instance != first + i)
                    {
                        throw new SscmException($"Can't preserve non-contiguous inputs [{string.Join(", ", preservedInputs.Select(i => i.Instance))}].");
                    }
                }
            }
        }

        foreach (var mapping in source.Mappings.Where(m => m.Preserve))
        {
            // all preserved mappings need to reference a preserved input
            var s = mapping.Input.Split('_');
            if (s.Length < 2)
            {
                throw new SscmException($"Invalid mapping binding [{mapping.Input}].");
            }
            var prefix = s[0] + "_";
            if (!inputPrefixes.Contains(prefix))
            {
                throw new SscmException($"Couldn't find related input for binding [{mapping.Input}].");
            }
        }
    }

    private async Task<bool> Export(MappingData source, bool apply)
    {
        this.Validate(source);

        this._xml = await ActionMapsXmlHelper.Load(this.GameConfigPath, "default");

        var changed = this.ExportInputDevices(source.Inputs);
        changed = changed | this.ExportMappings(source.Mappings);

        if (apply)
        {
            this.StandardOutput("Saving updated actionmaps.xml...");
            await this._xml.Save(this.GameConfigPath);
            this.StandardOutput("Saved, run \"restore\" command to revert.");
            this.StandardOutput("MUST RESTART STAR CITIZEN FOR CHANGES TO TAKE AFFECT.");
        }

        return changed;
    }

    private bool RestoreInputs(IEnumerable<InputDevice> inputs)
    {
        var changed = false;
        // only restore joysticks and gamepads for now
        var preservedInputs = inputs.Where(i => (string.Equals("joystick", i.Type, StringComparison.OrdinalIgnoreCase) || string.Equals("gamepad", i.Type, StringComparison.OrdinalIgnoreCase)) && i.Preserve).ToList();
        var inputsToAdd = new List<InputDevice>();
        // map of current prefix => exported input, current options element, current rebind elements
        var inputsToRemap = new Dictionary<string, (InputDevice input, XElement options, IList<XElement> rebinds)>();
        var inputPrefixesToRemove = new List<string>();
        foreach (var exportedInput in preservedInputs)
        {
            var targetInputByInstance = this._xml.GetOptionsElementForInputTypeAndInstance(exportedInput);
            var targetInputByProduct = this._xml.GetOptionsElementForInputTypeAndProduct(exportedInput);

            if (targetInputByProduct != null)
            { // exported input exists
                if (string.Equals(exportedInput.Instance.ToString(), targetInputByProduct.GetAttribute("instance"), StringComparison.OrdinalIgnoreCase))
                { // 1-1: exported input matches
                    // do nothing
                    continue;
                }

                // 1-0: exported input has a different instance ID
                // remap bindings
                inputsToRemap[ActionMapsXmlHelper.GetInputPrefixForOptionsElement(targetInputByProduct)] = (exportedInput, targetInputByProduct, this._xml.GetAllActionRebindsForOptions(targetInputByProduct));
                // delete bindings that are in the way of the exported input instance
                inputPrefixesToRemove.Add(exportedInput.GetInputPrefix());
                continue;
            }
            
            if (targetInputByInstance != null)
            { // 0-1: exported input doesn't exist and something else has that instance ID
                // delete bindings that are in the way of the exported input instance
                inputPrefixesToRemove.Add(ActionMapsXmlHelper.GetInputPrefixForOptionsElement(targetInputByInstance));
            }
            else
            { // 0-0: exported input doesn't exist and nothing else has that instance ID
                inputsToAdd.Add(exportedInput);
            }
        }

        // delete via removeForPrefixes if not in remapOptionsMap.Keys
        foreach (var prefix in inputPrefixesToRemove.Where(p => !inputsToRemap.ContainsKey(p)))
        {
            changed = true;
            this.StandardOutput($"Removing mappings like [{prefix}]...");
            this._xml.GetAllActionRebindsForInputPrefix(prefix).ForEach(rebind => rebind.Parent.Remove());
            var (type, instance) = ActionMapsXmlHelper.GetOptionsTypeAndInstanceForPrefix(prefix);
            this.StandardOutput($"Removing input for {type} {instance}...");
            this._xml.GetOptionsElementForInputTypeAndInstance(type, instance).Remove();
        }

        // remap via remapOptionsMap
        foreach (var oldPrefixAndElements in inputsToRemap)
        {
            changed = true;
            var oldPrefix = oldPrefixAndElements.Key;
            var (exportedInput, optionsElementToUpdate, rebindElementsToUpdate) = oldPrefixAndElements.Value;
            var newPrefix = exportedInput.GetInputPrefix();
            this.StandardOutput($"Rebinding mappings from [{oldPrefix}] to [{newPrefix}]...");
            foreach (var rebind in rebindElementsToUpdate)
            {
                rebind.SetAttributeValue("input", rebind.GetAttribute("input").Replace(oldPrefix, newPrefix));
            }
            
            optionsElementToUpdate.SetAttributeValue("instance", exportedInput.Instance);
        }

        // add missing inputs
        foreach (var input in inputsToAdd)
        {
            changed = true;
            var options = new XElement("options", new XAttribute("type", input.Type), new XAttribute("instance", input.Instance.ToString()), new XAttribute("Product", input.Product));
            this.DebugOutput($"Creating {options.ToString()}...");
            this.StandardOutput($"Restoring {input.Type}-{input.Instance} input [{input.Product}]");
            this._xml.AddOptionsElement(options);
        }

        return changed;
    }

    private bool ExportInputDevices(IEnumerable<InputDevice> inputs)
    {
        var changed = this.RestoreInputs(inputs);

        foreach (var input in inputs)
        {
            foreach (var setting in input.Settings.Where(s => s.Preserve))
            {
                var settingElement = this._xml.GetElementForInputSetting(input, setting.Name);
                if (settingElement == null)
                {
                    var inputElement = this._xml.GetOptionsElementForInputDevice(input);
                    if (inputElement == null)
                    {
                        throw new SscmException($"Could not find <options> element for type [{input.Type}] instance [{input.Instance}] Product [{input.Product}].");
                    }

                    changed = true;
                    this.DebugOutput($"Creating <{setting.Name}>...");
                    // create setting element
                    settingElement = new XElement(setting.Name);
                    inputElement.Add(settingElement);
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
                        
                        changed = true;
                        this.StandardOutput($"Updating {input.Product}/{setting.Name}/{prop.Value}...");
                        settingValueElement = XElement.Parse(prop.Value);
                        settingElement.Add(settingValueElement);
                    }
                    else if (!string.Equals(settingElement.GetAttribute(prop.Key), prop.Value))
                    {
                        changed = true;
                        // handle attribute property
                        this.StandardOutput($"Updating {input.Product}/{setting.Name}/{prop.Key} to {prop.Value}...");
                        settingElement.SetAttributeValue(prop.Key, prop.Value);                    
                    }
                }
            }
        }

        return changed;
    }

    private bool ExportMappings(IEnumerable<Mapping> mappings)
    {
        var changed = false;
        foreach (var mapping in mappings.Where(m => m.Preserve))
        {
            var actionElement = this._xml.GetActionForMapping(mapping);
            if (actionElement == null)
            {
                var actionmapElement = this._xml.GetActionmapForMapping(mapping);
                if (actionmapElement == null)
                {
                    this.DebugOutput($"Creating <actionmap name=\"{mapping.ActionMap}\">...");
                    // create <actionmap>
                    actionmapElement = new XElement("actionmap");
                    actionmapElement.SetAttributeValue("name", mapping.ActionMap);
                    this._xml.AddActionmapElement(actionmapElement);
                }

                changed = true;
                this.DebugOutput($"Creating <action name=\"{mapping.Action}\">...");
                // create <action>
                actionElement = new XElement("action");
                actionElement.SetAttributeValue("name", mapping.Action);
                actionmapElement.Add(actionElement);
            }

            var rebindElement = actionElement.GetChildren("rebind").SingleOrDefault(r => (r.GetAttribute("input") ?? string.Empty).StartsWith(ActionMapsXmlHelper.GetOptionsTypeAbbv(mapping.InputType)));
            if (rebindElement == null)
            {
                changed = true;
                this.DebugOutput($"Creating <rebind input=\"{mapping.Input}\" />...");
                this.StandardOutput($"Adding {mapping.ActionMap}/{mapping.Action} for {mapping.Input}...");
                rebindElement = new XElement("rebind");
                rebindElement.SetAttributeValue("input", mapping.Input);
                actionElement.Add(rebindElement);
            }
            else
            {
                if (!string.Equals(rebindElement.GetAttribute("input"), mapping.Input))
                {
                    changed = true;
                    this.StandardOutput($"Updating {mapping.ActionMap}/{mapping.Action} to {mapping.Input}...");
                    rebindElement.SetAttributeValue("input", mapping.Input);
                }
            }
        }
        
        return changed;
    }
}