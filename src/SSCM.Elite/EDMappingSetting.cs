namespace SSCM.Elite;

public class EDMappingSetting
{
    [Newtonsoft.Json.JsonIgnore]
    public string Id => $"{Group}-{Name}";

    public string Group { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public bool Preserve { get; set; }
}