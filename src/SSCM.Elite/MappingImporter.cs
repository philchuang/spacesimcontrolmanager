using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.Elite;

public class MappingImporter : IMappingImporter<MappingData>
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string GameConfigPath { get; private set; }

    private readonly IPlatform _platform;

    private MappingData _data = new MappingData();

    public MappingImporter(IPlatform platform, string gameConfigPath)
    {
        this._platform = platform;
        this.GameConfigPath = gameConfigPath;
    }

    public async Task<MappingData> Read()
    {
        throw new NotImplementedException();
    }
}
