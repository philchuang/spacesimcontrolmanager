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
    
    public MappingUpdater(IPlatform platform, string actionmapsxmlpath)
    {
        this._platform = platform;
        this.ActionMapsXmlPath = actionmapsxmlpath;
    }

    public async Task Backup()
    {
        if (!System.IO.File.Exists(this.ActionMapsXmlPath))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{this.ActionMapsXmlPath}]!");
        }

        // make backup of actionmaps.xml
        var actionmapsxmlBackup = $"{this.ActionMapsXmlPath}.{this._platform.UtcNow.ToLocalTime().ToString("yyyyMMddHHmmss")}.bak";
        System.IO.File.Copy(this.ActionMapsXmlPath, actionmapsxmlBackup);
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