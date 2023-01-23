namespace SSCM.Elite;

public class EDBindingKey
{
    [Newtonsoft.Json.JsonIgnore]
    public string Id => $"{Device}-{Key}";

    public string Device { get; set; }
    public string Key { get; set; }

    public override string ToString() => this.Id;
}