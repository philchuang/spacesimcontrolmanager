using Newtonsoft.Json;

namespace SSCM.StarCitizen;

public class SCMapping
{
    public string ActionMap { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public int? MultiTap { get; set; } = null;
    public bool Preserve { get; set; } = false;

    [JsonIgnore]
    public string Id => $"{this.ActionMap}-{this.Action}";
    [JsonIgnore]
    public string InputToString => $"{this.Input}{(this.MultiTap == null ? "" : $":{this.MultiTap}")}";
}