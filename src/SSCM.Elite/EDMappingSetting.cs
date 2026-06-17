namespace SSCM.Elite;

public class EDMappingSetting
{
    [Newtonsoft.Json.JsonIgnore]
    public string Id => $"{Group}.{Name}";

    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Preserve { get; set; }

    public EDMappingSetting()
    {
    }

    public EDMappingSetting(string group, string name, string value, bool preserve = true)
    {
        this.Group = group;
        this.Name = name;
        this.Value = value;
        this.Preserve = preserve;
    }
}
