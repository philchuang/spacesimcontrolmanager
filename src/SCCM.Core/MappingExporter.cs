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

        await this.ExportInputDevices(data.Inputs, apply);
        await this.ExportMappings(data.Mappings, apply);
    }

    private XDocument? _xd;
    private XElement? _actionMapsElement;
    private XElement? _actionProfilesDefaultElement;
    private Dictionary<string, XElement> _actionElementMap;

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

        // TODO not quite
        this._actionElementMap = this._actionProfilesDefaultElement.GetChildren("actionmaps").Where(e => e.GetAttribute("name") != string.Empty).ToDictionary(e => e.GetAttribute("name"));
    }

    private async Task ExportInputDevices(IEnumerable<InputDevice> inputs, bool apply)
    {
    }

    private XElement GetActionElement(string actionmapName, string actionName)
    {
        // silly code to prevent warning
        if (this._actionProfilesDefaultElement == null) throw new Exception();

        return this._actionProfilesDefaultElement
            .GetChildren("actionmap").SingleOrDefault(actionmap => actionmap.GetAttribute("name") == actionmapName)
            .GetChildren("action").SingleOrDefault(action => action.GetAttribute("name") == actionName);
    }

    private async Task ExportMappings(IEnumerable<Mapping> mappings, bool apply)
    {
        // TODO implement
    }
}