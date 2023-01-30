namespace SSCM.Elite;

public class EDMapping
{
    [Newtonsoft.Json.JsonIgnore]
    public string Id => $"{Group}.{Name}";
    [Newtonsoft.Json.JsonIgnore]
    public bool AnyPreserve => this.Binding?.Preserve == true || this.Primary?.Preserve == true || this.Secondary?.Preserve == true || this.Settings.Any(s => s.Preserve);

    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EDBinding? Binding { get; set; } = null;
    public EDBinding? Primary { get; set; } = null;
    public EDBinding? Secondary { get; set; } = null;
    public IList<EDMappingSetting> Settings { get; set; } = new List<EDMappingSetting>();

    public EDMapping() {}

    public EDMapping(string group, string name)
    {
        this.Group = group;
        this.Name = name;
    }

    public EDBinding? GetBinding(string type) => type switch {
        nameof(EDMapping.Binding) => this.Binding,
        nameof(EDMapping.Primary) => this.Primary,
        nameof(EDMapping.Secondary) => this.Secondary,
        _ => throw new ArgumentOutOfRangeException(),
    };
}