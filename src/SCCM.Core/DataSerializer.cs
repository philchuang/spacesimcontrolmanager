using Newtonsoft.Json;
using System.Collections;
using System.Linq;
using static SCCM.Core.Extensions;

namespace SCCM.Core;

public class DataSerializer
{
    public event Action<string> StandardOutput = delegate {};
    public event Action<string> WarningOutput = delegate {};
    public event Action<string> DebugOutput = delegate {};

    public string SavePath { get; private set; }

    public Formatting Formatting { get; set; } = Formatting.Indented;

    public DataSerializer(string path)
    {
        this.SavePath = path;
    }

    public async Task Write(MappingData data)
    {
        using (var fs = File.Open(this.SavePath, FileMode.Create))
        using (var sw = new StreamWriter(fs))
        {
            await sw.WriteAsync(this.ToJson(data));
        }
    }

    public string ToJson(object data)
    {
        return JsonConvert.SerializeObject(data, this.Formatting);
    }

    public async Task<MappingData?> Read()
    {
        if (!System.IO.File.Exists(this.SavePath))
        {
            this.StandardOutput($"Could not find the Star Citizen Control Mapper mappings file at [{this.SavePath}]!");
            return null;
        }

        return JsonConvert.DeserializeObject<MappingData>(await File.ReadAllTextAsync(this.SavePath));
    }
}
