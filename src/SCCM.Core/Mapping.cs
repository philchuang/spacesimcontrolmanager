using System.Text.RegularExpressions;

namespace SCCM.Core;

public class Mapping
{
    public string ActionMap { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public int? MultiTap { get; set; } = null;
    public bool Preserve { get; set; } = false;

    private static Regex REGEX = new Regex(@"^([A-z]+)(\d+)_(.+)$");

    public (string Type, int Instance, string bind)? GetInputTypeAndInstance()
    {
        var match = REGEX.Match(this.Input);
        if (!match.Success) return null;

        var type = match.Groups[1].Value switch {
            "kb" => "keyboard",
            "js" => "joystick",
            _ => throw new ArgumentOutOfRangeException(),
        };
        var instance = int.Parse(match.Groups[2].Value);
        var bind = match.Groups[3].Value;

        return (type, instance, bind);
    }
}