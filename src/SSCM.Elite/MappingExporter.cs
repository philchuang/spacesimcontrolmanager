using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingExporter : MappingExporterBase<EDMappingData>
{
    private string GameMappingsPath => this._folders.GameConfigPath;

    private readonly IEDFolders _folders;
    private CustomBindsXmlHelper? _xml;
    
    public MappingExporter(IPlatform platform, IEDFolders folders): base(platform)
    {
        this._folders = folders;
    }

    public override string Backup() => base.Backup(this.GameMappingsPath, this._folders.EliteDataDir);

    public override string RestoreLatest() => base.RestoreLatest(this._folders.EliteDataDir, $"{Path.GetFileName(this.GameMappingsPath)}.*.bak", this.GameMappingsPath);

    public override async Task<bool> Preview(EDMappingData source)
    {
        var session = await this.CreateInteractiveSession(source);
        foreach (var row in session.Rows)
        {
            base._StandardOutput($"{row.ChangeKind}: {row.ItemId} from {row.CurrentValue} to {row.NewValue}.");
        }
        return session.HasRows;
    }

    public override async Task<bool> Update(EDMappingData source)
    {
        var session = await this.CreateInteractiveSession(source);
        var changed = session.HasRows;
        session.SelectAll();
        session.ApplySelected();
        if (changed)
        {
            await this.SaveInteractive();
        }
        return changed;
    }

    public override async Task<InteractiveChangeSession> CreateInteractiveSession(EDMappingData source)
    {
        this.Validate(source);
        this._xml = await CustomBindsXmlHelper.Load(this._folders.GameConfigPath);
        var rows = new List<InteractiveChangeRow>();

        foreach (var m in source.Mappings.Where(m => m.AnyPreserve))
        {
            var mappingElement = this._xml.Xml.Root!.Element(m.Name);
            AddBindingRow(m.Binding, nameof(m.Binding), m.Id, mappingElement);
            AddBindingRow(m.Primary, nameof(m.Primary), m.Id, mappingElement);
            AddBindingRow(m.Secondary, nameof(m.Secondary), m.Id, mappingElement);

            foreach (var s in m.Settings.Where(s => s.Preserve))
            {
                var current = mappingElement?.Element(s.Name)?.GetAttribute("Value") ?? "";
                if (!string.Equals(current, s.Value))
                {
                    rows.Add(new InteractiveChangeRow(s.Id, "Update", s.Id, current, s.Value, true, () => this.ApplySetting(this._xml!.GetOrCreateMapping(m.Name).GetOrCreate(s.Name), s)));
                }
            }
        }

        foreach (var s in source.Settings.Where(s => s.Preserve))
        {
            var current = this._xml.Xml.Root!.Element(s.Name)?.GetAttribute("Value") ?? "";
            if (!string.Equals(current, s.Value))
            {
                rows.Add(new InteractiveChangeRow(s.Id, "Update", s.Id, current, s.Value, true, () => this.ApplySetting(this._xml!.GetOrCreateMapping(s.Name), s)));
            }
        }

        return new InteractiveChangeSession(rows);

        void AddBindingRow(EDBinding? binding, string type, string mappingId, XElement? mappingElement)
        {
            if (binding == null || !binding.Preserve) return;
            var bindingElement = mappingElement?.Element(type);
            var current = "";
            if (bindingElement != null)
            {
                var (device, key) = ReadBindingElement(bindingElement);
                current = $"{device}-{key}";
                var modifierElements = bindingElement.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (modifierElements.Any())
                {
                    current += " + " + string.Join(" + ", modifierElements.Select(ReadBindingElement).Select(((string, string) t) => $"{t.Item1}-{t.Item2}"));
                }
            }
            if (!string.Equals(current, binding.ToString()))
            {
                var rowId = $"{mappingId}.{type}";
                rows.Add(new InteractiveChangeRow(rowId, "Update", rowId, current, binding.ToString(), true, () => this.ApplyBinding(this._xml!.GetOrCreateMapping(mappingElement?.Name.LocalName ?? mappingId.Split('.').Last()).GetOrCreate(type), binding, mappingId)));
            }
        }
    }

    public override async Task SaveInteractive()
    {
        if (this._xml == null) throw new InvalidOperationException("No interactive export session has been created.");
        base._StandardOutput($"Saving updated {Path.GetFileName(this.GameMappingsPath)}...");
        await this._xml.Save(this.GameMappingsPath);
        base._StandardOutput("Saved, run \"restore\" command to revert.");
        base._StandardOutput("MUST RESTART ELITE DANGEROUS FOR CHANGES TO TAKE EFFECT.");
    }

    private void Validate(EDMappingData source)
    {
    }

    private async Task<bool> Export(EDMappingData source, bool apply)
    {
        // TODO implement ExportOptions
        this.Validate(source);

        this._xml = await CustomBindsXmlHelper.Load(this._folders.GameConfigPath);

        var changed = this.ExportMappings(source.Mappings);
        changed = changed | this.ExportSettings(source.Settings);

        if (apply)
        {
            base._StandardOutput($"Saving updated {Path.GetFileName(this.GameMappingsPath)}...");
            await this._xml.Save(this.GameMappingsPath);
            base._StandardOutput("Saved, run \"restore\" command to revert.");
            base._StandardOutput("MUST RESTART ELITE DANGEROUS FOR CHANGES TO TAKE EFFECT.");
        }

        return changed;
    }

    private bool ExportMappings(IList<EDMapping> mappings)
    {
        var changed = false;
        foreach (var m in mappings.Where(m => m.AnyPreserve))
        {
            changed |= ApplyMapping(m);
        }
        return changed;
    }

    private bool ApplyMapping(EDMapping mapping)
    {
        var changed = false;
        var xe = this._xml!.GetOrCreateMapping(mapping.Name);
        var func = (EDBinding? b, string type, string mappingId) => b != null && b.Preserve && ApplyBinding(xe.GetOrCreate(type), b, mappingId);
        changed |= func(mapping.Binding, nameof(mapping.Binding), mapping.Id);
        changed |= func(mapping.Primary, nameof(mapping.Primary), mapping.Id);
        changed |= func(mapping.Secondary, nameof(mapping.Secondary), mapping.Id);

        foreach (var s in mapping.Settings.Where(s => s.Preserve))
        {
            changed |= ApplySetting(xe.GetOrCreate(s.Name), s);
        }
        return changed;
    }

    private (string, string) ReadBindingElement(XElement bindingElement)
    {
        var device = bindingElement.GetAttribute("Device");
        var key = bindingElement.GetAttribute("Key");
        return (device, key);
    }

    private bool ApplyBinding(XElement bindingElement, EDBinding binding, string mappingId)
    {
        var changed = false;
        var (device, key) = ReadBindingElement(bindingElement);
        var currentBinding = $"{device}-{key}";

        if (!string.Equals(device, binding.Key.Device))
        {
            changed = true;
            bindingElement.SetAttributeValue("Device", binding.Key.Device);
        }
        if (!string.Equals(key, binding.Key.Key))
        {
            changed = true;
            bindingElement.SetAttributeValue("Key", binding.Key.Key);
        }
        
        if (binding.Modifiers.Any())
        {
            var modifierElements = bindingElement.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (modifierElements.Any())
            {
                currentBinding += " + " + 
                    string.Join(" + ", modifierElements.Select(ReadBindingElement).Select(((string, string) t) => $"{t.Item1}-{t.Item2}"));
            }
            var modifiers = new HashSet<string>(modifierElements.Select(ReadBindingElement).Select(((string, string) t) => $"{t.Item1}-{t.Item2}"));
            if (modifiers.Count != binding.Modifiers.Count
                || binding.Modifiers.Any(k => !modifiers.Contains(k.Id)))
            {
                changed = true;
                modifierElements.ForEach(m => m.Remove());
                foreach (var k in binding.Modifiers)
                {
                    var m = new XElement("Modifier");
                    bindingElement.Add(m);
                    m.SetAttributeValue("Device", k.Device);
                    m.SetAttributeValue("Key", k.Key);
                }
            }
        }

        if (changed)
        {
            base._StandardOutput($"Updated {mappingId}.{bindingElement.Name.LocalName} from {currentBinding} to {binding.ToString()}.");
        }

        return changed;
    }

    private bool ApplySetting(XElement settingElement, EDMappingSetting setting)
    {
        var value = settingElement.GetAttribute("Value");
        if (string.Equals(setting.Value, value))
        {
            return false;
        }
        
        settingElement.SetAttributeValue("Value", setting.Value);
        base._StandardOutput($"Updated {setting.Id} from {value} to {setting.Value}.");
        return true;
    }

    private bool ExportSettings(IList<EDMappingSetting> settings)
    {
        var changed = false;
        foreach (var s in settings.Where(s => s.Preserve))
        {
            var xe = this._xml!.GetOrCreateMapping(s.Name);
            changed |= ApplySetting(xe, s);
        }
        return changed;
   }
}
