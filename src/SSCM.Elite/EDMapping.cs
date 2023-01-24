namespace SSCM.Elite;

public class EDMapping
{
    [Newtonsoft.Json.JsonIgnore]
    public string Id => $"{Group}-{Name}";

    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EDBinding? Primary { get; set; }
    public EDBinding? Secondary { get; set; }
    public IList<EDMappingSetting> Settings { get; set; } = new List<EDMappingSetting>();

    public bool AnyPreserve => this.Primary?.Preserve == true || this.Secondary?.Preserve == true || this.Settings.Any(s => s.Preserve);
}