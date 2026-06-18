using Newtonsoft.Json;

namespace SSCM.StarCitizen;

public class UpgradeMapping
{
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;

    public async static Task<IList<UpgradeMapping>> Load(string path)
    {
        return JsonConvert.DeserializeObject<UpgradeMapping[]>(await File.ReadAllTextAsync(path)) ?? new UpgradeMapping[0];
    }

}
