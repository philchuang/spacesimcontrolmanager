namespace SSCM.Elite;

public class EDMapping
{
    [Newtonsoft.Json.JsonIgnore]
    public string Id => $"{Group}-{Name}";
    [Newtonsoft.Json.JsonIgnore]
    public bool AnyPreserve => this.Primary?.Preserve == true || this.Secondary?.Preserve == true || this.Settings.Any(s => s.Preserve);

    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EDBinding Primary { get; set; } = EDBinding.EMPTY();
    public EDBinding Secondary { get; set; } = EDBinding.EMPTY();
    public IList<EDMappingSetting> Settings { get; set; } = new List<EDMappingSetting>();
}