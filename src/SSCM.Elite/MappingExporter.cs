using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingExporter : IMappingExporter<EDMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string GameConfigPath => this._folders.GameConfigPath;

    private readonly IPlatform _platform;
    private readonly IEDFolders _folders;
    private CustomBindsXmlHelper? _xml;

    
    public MappingExporter(IPlatform platform, IEDFolders folders)
    {
        this._platform = platform;
        this._folders = folders;
    }

    public string Backup()
    {
        if (!File.Exists(this.GameConfigPath))
        {
            throw new FileNotFoundException($"Could not find the Elite Dangerous mappings file at [{this.GameConfigPath}]!");
        }

        // make backup
        Directory.CreateDirectory(this._folders.EliteDataDir);
        var backupPath = Path.Combine(this._folders.EliteDataDir, $"{Path.GetFileName(this.GameConfigPath)}.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak");
        File.Copy(this.GameConfigPath, backupPath);
        return backupPath;
    }

    public string RestoreLatest()
    {
        // find all files matching pattern, sort ordinally
        var backups = Directory.GetFiles(this._folders.EliteDataDir, $"{Path.GetFileName(this.GameConfigPath)}.*.bak");
        var latest = backups.OrderBy(s => s).LastOrDefault();
        if (latest == null)
        {
            throw new FileNotFoundException($"Could not find any backup files in [{this._folders.EliteDataDir}]!");
        }

        // copy latest file to actionmaps.xml
        File.Copy(latest, this.GameConfigPath, true);

        return latest;
    }

    public async Task<bool> Preview(EDMappingData source)
    {
        return await this.Export(source, false);
    }

    public async Task<bool> Update(EDMappingData source)
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
            this.StandardOutput($"Saving updated {Path.GetFileName(this.GameConfigPath)}...");
            await this._xml.Save(this.GameConfigPath);
            this.StandardOutput("Saved, run \"restore\" command to revert.");
            this.StandardOutput("MUST RESTART ELITE DANGEROUS FOR CHANGES TO TAKE AFFECT.");
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
        var func = (EDBinding? b, string name) => b != null && b.Preserve && ApplyBinding(xe.GetOrCreate(name), b);
        changed |= func(mapping.Binding, nameof(mapping.Binding));
        changed |= func(mapping.Primary, nameof(mapping.Primary));
        changed |= func(mapping.Secondary, nameof(mapping.Secondary));

        foreach (var s in mapping.Settings.Where(s => s.Preserve))
        {
            changed |= ApplySetting(xe.GetOrCreate(s.Name), s);
        }
        return changed;
    }

    private bool ApplyBinding(XElement bindingElement, EDBinding binding)
    {
        var changed = false;
        var device = bindingElement.GetAttribute("Device");
        var key = bindingElement.GetAttribute("Key");

        if (!string.Equals(device, binding.Key.Device))
        {
            changed = true;
            bindingElement.SetAttributeValue("Device", binding.Key.Device);
        }
        if (!string.Equals(device, binding.Key.Key))
        {
            changed = true;
            bindingElement.SetAttributeValue("Key", binding.Key.Key);
        }
        
        if (binding.Modifiers.Any())
        {
            var modifierElements = bindingElement.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase)).ToList();
            var modifiers = new HashSet<string>(modifierElements.Select(m => new EDBindingKey(m.GetAttribute("Device"), m.GetAttribute("Key")).Id));
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
        return true;
    }

    private bool ExportSettings(IList<EDMappingSetting> settings)
    {
         var changed = false;
        foreach (var s in settings.Where(s => s.Preserve))
        {
            var xe = this._xml.GetOrCreateMapping(s.Name);
            changed |= ApplySetting(xe, s);
        }
        return changed;
   }
}