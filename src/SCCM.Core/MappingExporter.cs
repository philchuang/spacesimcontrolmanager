using System.Linq;
using System.Xml;
using System.Xml.Linq;
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

        await this.ExportInputDevices(data.Inputs);
        await this.ExportMappings(data.Mappings);

        if (!apply) return;

        this.StandardOutput($"Saving new actionmaps.xml...");
        using (var fs = new FileStream(this.ActionMapsXmlPath, FileMode.Create))
        using (var xw = XmlWriter.Create(fs, new XmlWriterSettings { Async = true, Indent = true }))
        {
            var ct = new CancellationToken();
            await this._xd.WriteToAsync(xw, ct);
        }
        this.StandardOutput($"Saved.");
    }

    private XDocument? _xd;
    private XElement? _actionMapsElement;
    private XElement? _actionProfilesDefaultElement;
    private Dictionary<string, XElement> _inputElementMap = new Dictionary<string, XElement>();
    private Dictionary<string, XElement> _actionElementMap = new Dictionary<string, XElement>();

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
        this._actionMapsElement = this._xd.Root;
        this._actionProfilesDefaultElement = this._actionMapsElement.GetChildren("ActionProfiles").Single(ap => ap.GetAttribute("profileName") == "default");
        if (this._actionProfilesDefaultElement == null)
        {
            throw new InvalidDataException($"Could not find <ActionProfiles> with profileName [default].");
        }

        this._inputElementMap.Clear();
        foreach (var inputElement in this._actionProfilesDefaultElement.GetChildren("options").Where(e => e.GetAttribute("Product") != string.Empty))
        {
            var inputType = inputElement.GetAttribute("type");
            var inputInstance = inputElement.GetAttribute("instance");
            var inputProduct = inputElement.GetAttribute("Product");
            this._inputElementMap[$"{inputType}-{inputInstance}-{inputProduct}"] = inputElement;
            inputElement.Elements().ToList().ForEach(se => this._inputElementMap[$"{inputType}-{inputInstance}-{inputProduct}-{se.Name.LocalName}"] = se);
        }

        this._actionElementMap.Clear();
        foreach (var actionmapElement in this._actionProfilesDefaultElement.GetChildren("actionmap").Where(e => e.GetAttribute("name") != string.Empty))
        {
            var actionmapName = actionmapElement.GetAttribute("name");
            this._actionElementMap[$"{actionmapName}-"] = actionmapElement;
            foreach (var actionElement in actionmapElement.GetChildren("action").Where(e => e.GetAttribute("name") != string.Empty))
            {
                var actionName = actionElement.GetAttribute("name");
                this._actionElementMap[$"{actionmapName}-{actionName}"] = actionElement;
            }
        }
    }

    private async Task ExportInputDevices(IEnumerable<InputDevice> inputs)
    {
        foreach (var input in inputs)
        {
            foreach (var setting in input.Settings.Where(s => s.Preserve))
            {
                if (!this._inputElementMap.TryGetValue($"{input.Type}-{input.Instance}-{input.Product}-{setting.Name}", out var settingElement))
                {
                    if (!this._inputElementMap.TryGetValue($"{input.Type}-{input.Instance}-{input.Product}", out var inputElement))
                    {
                        throw new SccmException($"Could not find <options> element for type [{input.Type}] instance [{input.Instance}] Product [{input.Product}].");
                    }

                    this.StandardOutput($"Creating <{setting.Name}>...");
                    // create setting element
                    settingElement = new XElement(setting.Name);
                    inputElement.Add(settingElement);
                    this._inputElementMap[$"{input.Type}-{input.Instance}-{input.Product}-{setting.Name}"] = settingElement;
                }

                // TODO handle XML property
                foreach (var prop in setting.Properties)
                {
                    if (!string.Equals(settingElement.GetAttribute(prop.Key), prop.Value))
                    {
                        this.StandardOutput($"Updating {input.Product}-{setting.Name}-{prop.Key} to {prop.Value}...");
                        settingElement.SetAttributeValue(prop.Key, prop.Value);                    
                    }
                }
            }
        }
    }

    private async Task ExportMappings(IEnumerable<Mapping> mappings)
    {
        foreach (var mapping in mappings.Where(m => m.Preserve))
        {
            if (!this._actionElementMap.TryGetValue($"{mapping.ActionMap}-{mapping.Action}", out var actionElement))
            {
                if (!this._actionElementMap.TryGetValue($"{mapping.ActionMap}-", out var actionmapElement))
                {
                    this.StandardOutput($"Creating <actionmap name=\"{mapping.ActionMap}\">...");
                    // create <actionmap>
                    actionmapElement = new XElement("actionmap");
                    actionmapElement.SetAttributeValue("name", mapping.ActionMap);
                    this._actionProfilesDefaultElement.Add(actionmapElement);
                    this._actionElementMap[$"{mapping.ActionMap}-"] = actionmapElement;
                }

                this.StandardOutput($"Creating <action name=\"{mapping.Action}\">...");
                // create <action>
                actionElement = new XElement("action");
                actionElement.SetAttributeValue("name", mapping.Action);
                actionmapElement.Add(actionElement);
                this._actionElementMap[$"{mapping.ActionMap}-{mapping.Action}"] = actionmapElement;
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