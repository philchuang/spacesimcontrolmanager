using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace SCCM.Core;

public class MappingUpdater
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string ActionMapsXmlPath { get; private set; }

    private readonly IPlatform _platform;
    private readonly IFolders _folders;
    
    public MappingUpdater(IPlatform platform, IFolders folders, string actionmapsxmlpath)
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

    public async Task Update(MappingData data)
    {
        if (!System.IO.File.Exists(this.ActionMapsXmlPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.ActionMapsXmlPath}]!");
        }

        XDocument? xd = null;
        using (var fs = new FileStream(this.ActionMapsXmlPath, FileMode.Open))
        {
            var ct = new CancellationToken();
            xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);
        }

        // TODO
    }
}