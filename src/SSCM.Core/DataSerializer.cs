using Newtonsoft.Json;

namespace SSCM.Core;

public class DataSerializer<TData>
    where TData : class
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

    public async Task Write(TData data)
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

    public async Task<TData?> Read()
    {
        if (!System.IO.File.Exists(this.SavePath))
        {
            this.StandardOutput($"Could not find the SSCM mapping data file at [{this.SavePath}]!");
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<TData>(await File.ReadAllTextAsync(this.SavePath));
        }
        catch (JsonReaderException ex)
        {
            throw new SscmException($"Could not read SSCM mapping data file at [{this.SavePath}]!\nError: {ex.Message}", ex);
        }
    }
}
