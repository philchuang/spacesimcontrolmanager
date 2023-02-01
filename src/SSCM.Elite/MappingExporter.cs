using System.Text.RegularExpressions;
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
        return await this.Export(source, false);
    }

    public override async Task<bool> Update(EDMappingData source)
    {
        return await this.Export(source, true);
    }

    private void Validate(EDMappingData source)
    {
    }

    private async Task<bool> Export(EDMappingData source, bool apply)
    {
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