namespace SSCM.Core;

public class Mapping
{
    public string ActionMap { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public int? MultiTap { get; set; } = null;
    public bool Preserve { get; set; } = false;
}