namespace SSCM.StarCitizen;

public class SCInputDevice
{
    public string? Type { get; set; }
    public int Instance { get; set; }
    public string? Product { get; set; }
    public bool Preserve { get; set; }
    public IList<SCInputDeviceSetting> Settings { get; set; } = new List<SCInputDeviceSetting>();

    public string Id { get => $"{this.Type}-{this.Instance}-{this.Product}"; }
}