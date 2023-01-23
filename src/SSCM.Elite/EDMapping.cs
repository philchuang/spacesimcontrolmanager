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
}