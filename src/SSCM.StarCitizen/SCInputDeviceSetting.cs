namespace SSCM.StarCitizen;

public class SCInputDeviceSetting
{
    public string Name { get; set; } = string.Empty;
    public string Parent { get; set; } = string.Empty;
    public bool Preserve { get; set; }
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}
