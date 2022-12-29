namespace SCCM.Core;

public class InputDevice
{
    public string? Type { get; set; }
    public int Instance { get; set; }
    public string? Product { get; set; }
    public bool Preserve { get; set; }
    public IList<InputDeviceSetting> Settings { get; set; } = new List<InputDeviceSetting>();

    public string GetMappingPrefix()
    {
        var typeAbbv = this.Type switch {
            "joystick" => "js",
            "keyboard" => "kb",
            _ => throw new ArgumentOutOfRangeException(),
        };
        var prefix = $"{typeAbbv}{this.Instance}_";
        return prefix;
    }
}