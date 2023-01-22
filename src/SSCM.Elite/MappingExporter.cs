using System.Text.RegularExpressions;
using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingExporter : IMappingExporter<EDMappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string GameConfigPath { get; private set; }

    private readonly IPlatform _platform;
    
    public MappingExporter(IPlatform platform, string gameConfigPath)
    {
        this._platform = platform;
        this.GameConfigPath = gameConfigPath;
    }

    public string Backup()
    {
        throw new NotImplementedException();
    }

    public string RestoreLatest()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Preview(EDMappingData source)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Update(EDMappingData source)
    {
        throw new NotImplementedException();
    }

}