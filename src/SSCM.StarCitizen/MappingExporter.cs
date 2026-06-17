using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using SSCM.Core;
using static SSCM.StarCitizen.Extensions;

namespace SSCM.StarCitizen;

public class MappingExporter : MappingExporterBase<SCMappingData>
{
    private string GameAttributesPath => this._folders.GameAttributesPath;
    private string GameMappingsPath => this._folders.GameMappingsPath;

    private static Regex XML_REGEX = new Regex(@"<(\w[\w\d_]+)[^>]*>.+</\1>");

    private readonly ISCFolders _folders;
    private ActionMapsXmlHelper? _mappingsXml;
    private XDocument? _attributesXml;
    private ActionMapsXmlHelper MappingsXml => this._mappingsXml ?? throw new InvalidOperationException("No mappings export session has been created.");
    private XDocument AttributesXml => this._attributesXml ?? throw new InvalidOperationException("No attributes export session has been created.");
    private XElement AttributesRoot => this.AttributesXml.Root ?? throw new InvalidDataException("Expected an <Attributes> root element.");

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

    public override async Task<InteractiveChangeSession> CreateInteractiveSession(SCMappingData source)
    {
        this.Validate(source);
        this._mappingsXml = await ActionMapsXmlHelper.Load(this.GameMappingsPath, "default");
        this._attributesXml = await XmlExtensions.LoadAsync(this.GameAttributesPath);

        var rows = new List<InteractiveChangeRow>();
        AddInputRows(source.Inputs, rows);
        AddInputSettingRows(source.Inputs, rows);
        AddMappingRows(source.Mappings, rows);
        AddAttributeRows(source.Attributes, rows);
        return new InteractiveChangeSession(rows);
    }

    public override Task SaveInteractive()
    {
        if (this._mappingsXml == null || this._attributesXml == null) throw new InvalidOperationException("No interactive export session has been created.");
        base._StandardOutput("SAVING: updated actionmaps.xml...");
        this._mappingsXml.Save(this.GameMappingsPath);
        base._StandardOutput("SAVING: updated attributes.xml...");
        this._attributesXml.Save(this.GameAttributesPath);
        base._StandardOutput("Saved, run \"restore\" command to revert.");
        base._StandardOutput("MUST RESTART STAR CITIZEN FOR CHANGES TO TAKE EFFECT.");
        return Task.CompletedTask;
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

    private void AddInputRows(IEnumerable<SCInputDevice> inputs, IList<InteractiveChangeRow> rows)
    {
        var preservedInputs = inputs.Where(i => (string.Equals("joystick", i.Type, StringComparison.OrdinalIgnoreCase) || string.Equals("gamepad", i.Type, StringComparison.OrdinalIgnoreCase)) && i.Preserve).ToList();
        foreach (var input in preservedInputs)
        {
            var targetInputByInstance = this._mappingsXml!.GetOptionsElementForInputTypeAndInstance(input);
            var targetInputByProduct = this._mappingsXml.GetOptionsElementForInputTypeAndProduct(input);
            var newPrefix = input.GetInputPrefix();

            if (targetInputByProduct != null)
            {
                var oldPrefix = ActionMapsXmlHelper.GetInputPrefixForOptionsElement(targetInputByProduct);
                if (string.Equals(newPrefix, oldPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                var rebinds = this._mappingsXml.GetAllActionRebindsForOptions(targetInputByProduct);
                rows.Add(new InteractiveChangeRow(input.Id, "Update", input.Id, oldPrefix, newPrefix, true, () => {
                    foreach (var rebind in rebinds)
                    {
                        rebind.SetAttributeValue("input", rebind.GetAttribute("input").Replace(oldPrefix, newPrefix));
                    }
                    targetInputByProduct.SetAttributeValue("instance", input.Instance);
                    return true;
                }));

                if (targetInputByInstance != null && targetInputByInstance != targetInputByProduct)
                {
                    AddRemoveInputRow(rows, newPrefix, targetInputByInstance);
                }
                continue;
            }

            if (targetInputByInstance != null)
            {
                AddRemoveInputRow(rows, ActionMapsXmlHelper.GetInputPrefixForOptionsElement(targetInputByInstance), targetInputByInstance);
            }

            rows.Add(new InteractiveChangeRow(input.Id, "Add", input.Id, "", newPrefix, true, () => {
                var options = new XElement("options", new XAttribute("type", input.Type!), new XAttribute("instance", input.Instance.ToString()), new XAttribute("Product", input.Product!));
                this._mappingsXml!.AddOptionsElement(options);
                return true;
            }));
        }
    }

    private void AddRemoveInputRow(IList<InteractiveChangeRow> rows, string prefix, XElement options)
    {
        var rowId = $"input:{prefix}";
        rows.Add(new InteractiveChangeRow(rowId, "Remove", rowId, prefix, "", true, () => {
            this._mappingsXml!.GetAllActionRebindsForInputPrefix(prefix).ForEach(rebind => rebind.Remove());
            options.Remove();
            return true;
        }));
    }

    private void AddInputSettingRows(IEnumerable<SCInputDevice> inputs, IList<InteractiveChangeRow> rows)
    {
        foreach (var input in inputs)
        {
            foreach (var setting in input.Settings.Where(s => s.Preserve))
            {
                var settingElement = this._mappingsXml!.GetElementForInputSetting(input, setting.Name);
                foreach (var prop in setting.Properties)
                {
                    if (XML_REGEX.IsMatch(prop.Value))
                    {
                        var current = settingElement?.GetChildren(prop.Key).FirstOrDefault()?.ToString() ?? "";
                        if (string.Equals(current, prop.Value)) continue;
                        var rowId = $"{input.Id}.{setting.Name}.{prop.Key}";
                        rows.Add(new InteractiveChangeRow(rowId, "Update", rowId, current, prop.Value, true, () => {
                            var inputElement = this._mappingsXml!.GetOptionsElementForInputDevice(input) ?? throw new SscmException($"Could not find <options> element for type [{input.Type}] instance [{input.Instance}] Product [{input.Product}].");
                            var targetSetting = this._mappingsXml.GetElementForInputSetting(input, setting.Name) ?? new XElement(setting.Name);
                            if (targetSetting.Parent == null) inputElement.Add(targetSetting);
                            targetSetting.GetChildren(prop.Key).ToList().ForEach(e => e.Remove());
                            targetSetting.Add(XElement.Parse(prop.Value));
                            return true;
                        }));
                    }
                    else
                    {
                        var current = settingElement?.GetAttribute(prop.Key) ?? "";
                        if (string.Equals(current, prop.Value)) continue;
                        var rowId = $"{input.Id}.{setting.Name}.{prop.Key}";
                        rows.Add(new InteractiveChangeRow(rowId, "Update", rowId, current, prop.Value, true, () => {
                            var inputElement = this._mappingsXml!.GetOptionsElementForInputDevice(input) ?? throw new SscmException($"Could not find <options> element for type [{input.Type}] instance [{input.Instance}] Product [{input.Product}].");
                            var targetSetting = this._mappingsXml.GetElementForInputSetting(input, setting.Name) ?? new XElement(setting.Name);
                            if (targetSetting.Parent == null) inputElement.Add(targetSetting);
                            targetSetting.SetAttributeValue(prop.Key, prop.Value);
                            return true;
                        }));
                    }
                }
            }
        }
    }

    private void AddMappingRows(IEnumerable<SCMapping> mappings, IList<InteractiveChangeRow> rows)
    {
        foreach (var mapping in mappings.Where(m => m.Preserve))
        {
            var actionElement = this._mappingsXml!.GetActionForMapping(mapping);
            if (actionElement == null)
            {
                if (this.ExportOptions.OnlyMatches) continue;
                var rowId = $"{mapping.Id}.{mapping.InputType}";
                rows.Add(new InteractiveChangeRow(rowId, "Add", mapping.Id, "", mapping.InputToString, true, () => {
                    var actionmapElement = this._mappingsXml!.GetActionmapForMapping(mapping);
                    if (actionmapElement == null)
                    {
                        actionmapElement = new XElement("actionmap");
                        actionmapElement.SetAttributeValue("name", mapping.ActionMap);
                        this._mappingsXml.AddActionmapElement(actionmapElement);
                    }
                    var newAction = new XElement("action");
                    newAction.SetAttributeValue("name", mapping.Action);
                    actionmapElement.Add(newAction);
                    AddRebind(newAction, mapping);
                    return true;
                }));
                continue;
            }

            var rebindElement = actionElement.GetChildren("rebind").SingleOrDefault(r => (r.GetAttribute("input") ?? string.Empty).StartsWith(ActionMapsXmlHelper.GetOptionsTypeAbbv(mapping.InputType)));
            if (rebindElement == null)
            {
                var rowId = $"{mapping.Id}.{mapping.InputType}";
                rows.Add(new InteractiveChangeRow(rowId, "Add", mapping.Id, "", mapping.InputToString, true, () => {
                    AddRebind(actionElement, mapping);
                    return true;
                }));
                continue;
            }

            var currentValue = $"{rebindElement.GetAttribute("input")}{(!string.IsNullOrWhiteSpace(rebindElement.GetAttribute("multiTap")) ? $":{rebindElement.GetAttribute("multiTap")}" : "")}";
            if (!string.Equals(currentValue, mapping.InputToString))
            {
                var rowId = $"{mapping.Id}.{mapping.InputType}";
                rows.Add(new InteractiveChangeRow(rowId, "Update", mapping.Id, currentValue, mapping.InputToString, true, () => {
                    rebindElement.SetAttributeValue("input", mapping.Input);
                    if (mapping.MultiTap == null)
                    {
                        rebindElement.Attribute("multiTap")?.Remove();
                    }
                    else
                    {
                        rebindElement.SetAttributeValue("multiTap", mapping.MultiTap.ToString());
                    }
                    return true;
                }));
            }
        }
    }

    private static void AddRebind(XElement actionElement, SCMapping mapping)
    {
        var rebindElement = new XElement("rebind");
        rebindElement.SetAttributeValue("input", mapping.Input);
        if (mapping.MultiTap != null)
            rebindElement.SetAttributeValue("multiTap", mapping.MultiTap.ToString());
        actionElement.Add(rebindElement);
    }

    private void AddAttributeRows(IEnumerable<SCAttribute> attributes, IList<InteractiveChangeRow> rows)
    {
        foreach (var a in attributes.Where(a => a.Preserve))
        {
            var xe = this._attributesXml!.XPathSelectElement($"/Attributes/Attr[@name='{a.Name}']");
            if (xe == null && this.ExportOptions.OnlyMatches) continue;
            var current = xe?.GetAttribute("value") ?? "";
            if (string.Equals(a.Value, current)) continue;
            rows.Add(new InteractiveChangeRow(a.Name, xe == null ? "Add" : "Update", a.Name, current, a.Value, true, () => {
                var target = this.AttributesXml.XPathSelectElement($"/Attributes/Attr[@name='{a.Name}']");
                if (target == null)
                {
                    target = new XElement("Attr", new XAttribute("name", a.Name));
                    this.AttributesRoot.Add(target);
                }
                return ApplyAttribute(target, a);
            }));
        }
    }

    private async Task<bool> Export(SCMappingData source, bool apply)
    {
        // TODO write tests for ExportOptions.OnlyMatches
        // TODO implement interactive mode
        this.Validate(source);

        this._mappingsXml = await ActionMapsXmlHelper.Load(this.GameMappingsPath, "default");
        this._attributesXml = await XmlExtensions.LoadAsync(this.GameAttributesPath);

        var changed = this.ExportInputDevices(source.Inputs);
        changed |= this.ExportMappings(source.Mappings);
        changed |= this.ExportAttributes(source.Attributes);

        if (apply)
        {
            base._StandardOutput("SAVING: updated actionmaps.xml...");
            this._mappingsXml.Save(this.GameMappingsPath);
            base._StandardOutput("SAVING: updated attributes.xml...");
            this._attributesXml!.Save(this.GameAttributesPath);
            base._StandardOutput("Saved, run \"restore\" command to revert.");
            base._StandardOutput("MUST RESTART STAR CITIZEN FOR CHANGES TO TAKE EFFECT.");
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
            var targetInputByInstance = this.MappingsXml.GetOptionsElementForInputTypeAndInstance(exportedInput);
            var targetInputByProduct = this.MappingsXml.GetOptionsElementForInputTypeAndProduct(exportedInput);

            if (targetInputByProduct != null)
            { // exported input exists
                if (string.Equals(exportedInput.Instance.ToString(), targetInputByProduct.GetAttribute("instance"), StringComparison.OrdinalIgnoreCase))
                { // 1-1: exported input matches
                    // do nothing
                    continue;
                }

                // 1-0: exported input has a different instance ID
                // remap bindings
                inputsToRemap[ActionMapsXmlHelper.GetInputPrefixForOptionsElement(targetInputByProduct)] = (exportedInput, targetInputByProduct, this.MappingsXml.GetAllActionRebindsForOptions(targetInputByProduct));
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
            base._StandardOutput($"REMOVING: mappings like [{prefix}]...");
            this.MappingsXml.GetAllActionRebindsForInputPrefix(prefix).ForEach(rebind => rebind.Remove());
            var (type, instance) = ActionMapsXmlHelper.GetOptionsTypeAndInstanceForPrefix(prefix);
            base._StandardOutput($"REMOVING: input for {type} {instance}...");
            this.MappingsXml.GetOptionsElementForInputTypeAndInstance(type, instance)?.Remove();
        }

        // remap via remapOptionsMap
        foreach (var oldPrefixAndElements in inputsToRemap)
        {
            changed = true;
            var oldPrefix = oldPrefixAndElements.Key;
            var (exportedInput, optionsElementToUpdate, rebindElementsToUpdate) = oldPrefixAndElements.Value;
            var newPrefix = exportedInput.GetInputPrefix();
            base._StandardOutput($"UPDATING: mappings from [{oldPrefix}] to [{newPrefix}]...");
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
            base._DebugOutput($"CREATING: {options.ToString()}...");
            base._StandardOutput($"RESTORING: {input.Type}-{input.Instance} input [{input.Product}]");
            this.MappingsXml.AddOptionsElement(options);
        }

        return changed;
    }

    private bool ExportInputDevices(IEnumerable<SCInputDevice> inputs)
    {
        // TODO consider ExportOptions.OnlyMatches behavior
        var changed = this.RestoreInputs(inputs);

        foreach (var input in inputs)
        {
            foreach (var setting in input.Settings.Where(s => s.Preserve))
            {
                var settingElement = this.MappingsXml.GetElementForInputSetting(input, setting.Name);
                if (settingElement == null)
                {
                    var inputElement = this.MappingsXml.GetOptionsElementForInputDevice(input);
                    if (inputElement == null)
                    {
                        throw new SscmException($"Could not find <options> element for type [{input.Type}] instance [{input.Instance}] Product [{input.Product}].");
                    }

                    changed = true;
                    base._DebugOutput($"CREATING: <{setting.Name}>...");
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
                        base._StandardOutput($"UPDATING: {input.Product}/{setting.Name}/{prop.Value}...");
                        settingValueElement = XElement.Parse(prop.Value);
                        settingElement.Add(settingValueElement);
                    }
                    else if (!string.Equals(settingElement.GetAttribute(prop.Key), prop.Value))
                    {
                        changed = true;
                        // handle attribute property
                        base._StandardOutput($"UPDATING: {input.Product}/{setting.Name}/{prop.Key} to {prop.Value}...");
                        settingElement.SetAttributeValue(prop.Key, prop.Value);                    
                    }
                }
            }
        }

        return changed;
    }

    private bool ExportMappings(IEnumerable<SCMapping> mappings)
    {
        var anyChanged = false;
        foreach (var mapping in mappings.Where(m => m.Preserve))
        {
            // if (mapping.Input.EndsWith("_ "))
            // {
            //     base._DebugOutput($"SKIPPING: {mapping.ActionMap}/{mapping.Action} binding of {mapping.Input} appears to be empty.");
            //     continue;
            // }
            var actionElement = this.MappingsXml.GetActionForMapping(mapping);
            if (actionElement == null)
            { // mapping not present
                if (this.ExportOptions.OnlyMatches)
                {
                    base._StandardOutput($"SKIPPING: {mapping.ActionMap}/{mapping.Action} not present in mappings file.");
                    continue;
                }
                var actionmapElement = this.MappingsXml.GetActionmapForMapping(mapping);
                if (actionmapElement == null)
                {
                    base._DebugOutput($"CREATING: <actionmap name=\"{mapping.ActionMap}\">...");
                    // create <actionmap>
                    actionmapElement = new XElement("actionmap");
                    actionmapElement.SetAttributeValue("name", mapping.ActionMap);
                    this.MappingsXml.AddActionmapElement(actionmapElement);
                }

                anyChanged = true;
                base._DebugOutput($"CREATING: <action name=\"{mapping.Action}\">...");
                // create <action>
                actionElement = new XElement("action");
                actionElement.SetAttributeValue("name", mapping.Action);
                actionmapElement.Add(actionElement);
            }

            var rebindElement = actionElement.GetChildren("rebind").SingleOrDefault(r => (r.GetAttribute("input") ?? string.Empty).StartsWith(ActionMapsXmlHelper.GetOptionsTypeAbbv(mapping.InputType)));
            if (rebindElement == null)
            {
                anyChanged = true;
                base._DebugOutput($"CREATING: <rebind input=\"{mapping.Input}\" />...");
                base._StandardOutput($"ADDING: {mapping.Id} for {mapping.InputToString}...");
                rebindElement = new XElement("rebind");
                rebindElement.SetAttributeValue("input", mapping.Input);
                if (mapping.MultiTap != null)
                    rebindElement.SetAttributeValue("multiTap", mapping.MultiTap.ToString());
                actionElement.Add(rebindElement);
            }
            else
            {
                var thisChanged = false;
                var inputAttrValue = rebindElement.GetAttribute("input");
                if (!string.Equals(inputAttrValue, mapping.Input))
                {
                    anyChanged = true;
                    thisChanged = true;
                    rebindElement.SetAttributeValue("input", mapping.Input);
                }
                var multiTapAttrValue = rebindElement.GetAttribute("multiTap");
                if (!string.Equals(multiTapAttrValue, mapping.MultiTap?.ToString() ?? string.Empty))
                {
                    anyChanged = true;
                    thisChanged = true;
                    if (mapping.MultiTap == null)
                    {
                        rebindElement.Attribute("multiTap")!.Remove();
                    }
                    else
                    {
                        rebindElement.SetAttributeValue("multiTap", mapping.MultiTap.ToString());
                    }
                }

                if (thisChanged)
                {
                    var prevValue = $"{inputAttrValue}{(!string.IsNullOrWhiteSpace(multiTapAttrValue) ? $":{multiTapAttrValue}" : "")}";
                    base._StandardOutput($"UPDATING: {mapping.Id} from {prevValue} to {mapping.InputToString}...");
                }
            }
        }
        
        return anyChanged;
    }

    private bool ExportAttributes(IEnumerable<SCAttribute> attributes)
    {
        var changed = false;
        foreach (var a in attributes.Where(a => a.Preserve))
        {
            var xe = this._attributesXml!.XPathSelectElement($"/Attributes/Attr[@name='{a.Name}']");
            if (xe == null)
            {
                if (this.ExportOptions.OnlyMatches)
                {
                    base._StandardOutput($"SKIPPING: {a.Name} not present in attributes file.");
                    continue;
                }
                xe = new XElement("Attr", new XAttribute("name", a.Name));
                base._DebugOutput($"CREATING: <Attr name=\"{a.Name}\" />...");
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
        base._StandardOutput($"UPDATING: attribute {attr.Name} from [{value}] to [{attr.Value}]...");
        return true;
    }
}
