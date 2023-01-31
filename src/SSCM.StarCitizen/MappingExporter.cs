using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using SSCM.Core;
using static SSCM.StarCitizen.Extensions;

namespace SSCM.StarCitizen;

public class MappingExporter : MappingExporterBase<SCMappingData>
{
    private string GameAttributesPath => this._folders.GameAttributesPath;
    private string GameConfigDir => this._folders.GameConfigDir;
    private string GameMappingsPath => this._folders.GameMappingsPath;

    private static Regex XML_REGEX = new Regex(@"<(\w[\w\d_]+)[^>]*>.+</\1>");

    private readonly ISCFolders _folders;
    private ActionMapsXmlHelper? _mappingsXml;
    private XDocument? _attributesXml;

    public MappingExporter(IPlatform platform, ISCFolders folders) : base(platform)
    {
        this._folders = folders;
    }

    public override string Backup()
    {
        var mappings = base.Backup(this.GameMappingsPath, this._folders.ScDataDir);
        var attributes = base.Backup(this.GameAttributesPath, this._folders.ScDataDir);
        return $"{mappings},{attributes}";
    }

    public override string RestoreLatest()
    {
        var mappings = base.RestoreLatest(this._folders.ScDataDir, "actionmaps.xml.*.bak", this.GameMappingsPath);
        var attributes = base.RestoreLatest(this._folders.ScDataDir, "attributes.xml.*.bak", this.GameAttributesPath);

        return $"{mappings},{attributes}";
    }

    public override async Task<bool> Preview(SCMappingData source)
    {
        return await this.Export(source, false);
    }

    public override async Task<bool> Update(SCMappingData source)
    {
        return await this.Export(source, true);
    }

    private void Validate(SCMappingData source)
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

    private async Task<bool> Export(SCMappingData source, bool apply)
    {
        this.Validate(source);

        this._mappingsXml = await ActionMapsXmlHelper.Load(this.GameMappingsPath, "default");
        this._attributesXml = await XmlExtensions.LoadAsync(this.GameAttributesPath);

        var changed = this.ExportInputDevices(source.Inputs);
        changed |= this.ExportMappings(source.Mappings);
        changed |= this.ExportAttributes(source.Attributes);

        if (apply)
        {
            base._StandardOutput("Saving updated actionmaps.xml...");
            this._mappingsXml.Save(this.GameMappingsPath);
            base._StandardOutput("Saving updated attributes.xml...");
            this._attributesXml!.Save(this.GameAttributesPath);
            base._StandardOutput("Saved, run \"restore\" command to revert.");
            base._StandardOutput("MUST RESTART STAR CITIZEN FOR CHANGES TO TAKE AFFECT.");
        }

        return changed;
    }

    private bool RestoreInputs(IEnumerable<SCInputDevice> inputs)
    {
        var changed = false;
        // only restore joysticks and gamepads for now
        var preservedInputs = inputs.Where(i => (string.Equals("joystick", i.Type, StringComparison.OrdinalIgnoreCase) || string.Equals("gamepad", i.Type, StringComparison.OrdinalIgnoreCase)) && i.Preserve).ToList();
        var inputsToAdd = new List<SCInputDevice>();
        // map of current prefix => exported input, current options element, current rebind elements
        var inputsToRemap = new Dictionary<string, (SCInputDevice input, XElement options, IList<XElement> rebinds)>();
        var inputPrefixesToRemove = new List<string>();
        foreach (var exportedInput in preservedInputs)
        {
            var targetInputByInstance = this._mappingsXml.GetOptionsElementForInputTypeAndInstance(exportedInput);
            var targetInputByProduct = this._mappingsXml.GetOptionsElementForInputTypeAndProduct(exportedInput);

            if (targetInputByProduct != null)
            { // exported input exists
                if (string.Equals(exportedInput.Instance.ToString(), targetInputByProduct.GetAttribute("instance"), StringComparison.OrdinalIgnoreCase))
                { // 1-1: exported input matches
                    // do nothing
                    continue;
                }

                // 1-0: exported input has a different instance ID
                // remap bindings
                inputsToRemap[ActionMapsXmlHelper.GetInputPrefixForOptionsElement(targetInputByProduct)] = (exportedInput, targetInputByProduct, this._mappingsXml.GetAllActionRebindsForOptions(targetInputByProduct));
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
            base._StandardOutput($"Removing mappings like [{prefix}]...");
            this._mappingsXml.GetAllActionRebindsForInputPrefix(prefix).ForEach(rebind => rebind.Parent.Remove());
            var (type, instance) = ActionMapsXmlHelper.GetOptionsTypeAndInstanceForPrefix(prefix);
            base._StandardOutput($"Removing input for {type} {instance}...");
            this._mappingsXml.GetOptionsElementForInputTypeAndInstance(type, instance).Remove();
        }

        // remap via remapOptionsMap
        foreach (var oldPrefixAndElements in inputsToRemap)
        {
            changed = true;
            var oldPrefix = oldPrefixAndElements.Key;
            var (exportedInput, optionsElementToUpdate, rebindElementsToUpdate) = oldPrefixAndElements.Value;
            var newPrefix = exportedInput.GetInputPrefix();
            base._StandardOutput($"Rebinding mappings from [{oldPrefix}] to [{newPrefix}]...");
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
            base._DebugOutput($"Creating {options.ToString()}...");
            base._StandardOutput($"Restoring {input.Type}-{input.Instance} input [{input.Product}]");
            this._mappingsXml.AddOptionsElement(options);
        }

        return changed;
    }

    private bool ExportInputDevices(IEnumerable<SCInputDevice> inputs)
    {
        var changed = this.RestoreInputs(inputs);

        foreach (var input in inputs)
        {
            foreach (var setting in input.Settings.Where(s => s.Preserve))
            {
                var settingElement = this._mappingsXml.GetElementForInputSetting(input, setting.Name);
                if (settingElement == null)
                {
                    var inputElement = this._mappingsXml.GetOptionsElementForInputDevice(input);
                    if (inputElement == null)
                    {
                        throw new SscmException($"Could not find <options> element for type [{input.Type}] instance [{input.Instance}] Product [{input.Product}].");
                    }

                    changed = true;
                    base._DebugOutput($"Creating <{setting.Name}>...");
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
                        base._StandardOutput($"Updating {input.Product}/{setting.Name}/{prop.Value}...");
                        settingValueElement = XElement.Parse(prop.Value);
                        settingElement.Add(settingValueElement);
                    }
                    else if (!string.Equals(settingElement.GetAttribute(prop.Key), prop.Value))
                    {
                        changed = true;
                        // handle attribute property
                        base._StandardOutput($"Updating {input.Product}/{setting.Name}/{prop.Key} to {prop.Value}...");
                        settingElement.SetAttributeValue(prop.Key, prop.Value);                    
                    }
                }
            }
        }

        return changed;
    }

    private bool ExportMappings(IEnumerable<SCMapping> mappings)
    {
        var changed = false;
        foreach (var mapping in mappings.Where(m => m.Preserve))
        {
            var actionElement = this._mappingsXml.GetActionForMapping(mapping);
            if (actionElement == null)
            {
                var actionmapElement = this._mappingsXml.GetActionmapForMapping(mapping);
                if (actionmapElement == null)
                {
                    base._DebugOutput($"Creating <actionmap name=\"{mapping.ActionMap}\">...");
                    // create <actionmap>
                    actionmapElement = new XElement("actionmap");
                    actionmapElement.SetAttributeValue("name", mapping.ActionMap);
                    this._mappingsXml.AddActionmapElement(actionmapElement);
                }

                changed = true;
                base._DebugOutput($"Creating <action name=\"{mapping.Action}\">...");
                // create <action>
                actionElement = new XElement("action");
                actionElement.SetAttributeValue("name", mapping.Action);
                actionmapElement.Add(actionElement);
            }

            var rebindElement = actionElement.GetChildren("rebind").SingleOrDefault(r => (r.GetAttribute("input") ?? string.Empty).StartsWith(ActionMapsXmlHelper.GetOptionsTypeAbbv(mapping.InputType)));
            if (rebindElement == null)
            {
                changed = true;
                base._DebugOutput($"Creating <rebind input=\"{mapping.Input}\" />...");
                base._StandardOutput($"Adding {mapping.ActionMap}/{mapping.Action} for {mapping.Input}...");
                rebindElement = new XElement("rebind");
                rebindElement.SetAttributeValue("input", mapping.Input);
                actionElement.Add(rebindElement);
            }
            else
            {
                if (!string.Equals(rebindElement.GetAttribute("input"), mapping.Input))
                {
                    changed = true;
                    base._StandardOutput($"Updating {mapping.ActionMap}/{mapping.Action} to {mapping.Input}...");
                    rebindElement.SetAttributeValue("input", mapping.Input);
                }
            }
        }
        
        return changed;
    }

    private bool ExportAttributes(IEnumerable<SCAttribute> attributes)
    {
        var changed = false;
        foreach (var a in attributes.Where(a => a.Preserve))
        {
            var xe = this._attributesXml!.XPathSelectElement($"/Attributes/Attr[@name='{a.Name}']");
            if (xe == null)
            {
                xe = new XElement("Attr", new XAttribute("name", a.Name));
                base._DebugOutput($"Creating <Attr name=\"{a.Name}\" />...");
                this._attributesXml!.Root!.Add(xe);
            }
            changed |= ApplyAttribute(xe, a);
        }
        return changed;
    }

    private bool ApplyAttribute(XElement attrElement, SCAttribute attr)
    {
        var value = attrElement.GetAttribute("value");
        if (string.Equals(attr.Value, value)) return false;
        
        attrElement.SetAttributeValue("value", attr.Value);
        base._StandardOutput($"Updating attribute {attr.Name} from [{value}] to [{attr.Value}]...");
        return true;
    }
}