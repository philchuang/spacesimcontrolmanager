using System.Collections;
using System.Linq;
using static SCCM.Core.Extensions;

namespace SCCM.Core;

public class DataWriter
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string SavePath { get; private set; }

    public DataWriter(string path)
    {
        this.SavePath = path;
    }

    public async Task Write(MappingData data)
    {
    }
}
