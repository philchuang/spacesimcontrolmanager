namespace SSCM.Core;

public class CommandOption
{
    public string Name { get; init; } = string.Empty;

    public string ShortName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? DefaultValue { get; init; }
}
