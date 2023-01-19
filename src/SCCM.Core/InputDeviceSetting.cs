namespace SCCM.Core;

public class InputDeviceSetting
{
    public string Name { get; set; }
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}
