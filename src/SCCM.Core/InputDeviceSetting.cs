namespace SCCM.Core;

public class InputDeviceSetting
{
    public string Name { get; set; } = string.Empty;
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}
