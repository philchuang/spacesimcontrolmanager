namespace SSCM.Core;

/// <summary>
/// An output format a generator can print. The CLI adds a subcommand for each one, named <see cref="Name"/>.
/// </summary>
/// <param name="Name">The subcommand name, such as <c>markdown</c>. Matching is case-insensitive.</param>
/// <param name="Description">Help text for the subcommand.</param>
public record BindingPrintFormat(string Name, string Description);

/// <summary>
/// A yes/no switch a generator understands. The CLI adds an option named <c>--{Name}</c> to the print command.
/// </summary>
/// <param name="Name">The option name without dashes, such as <c>refresh-defaults</c>.</param>
/// <param name="Description">Help text for the option.</param>
public record BindingPrintFlag(string Name, string Description);

public class BindingPrintOptions
{
    /// <summary>The names of the <see cref="BindingPrintFlag"/>s that were switched on.</summary>
    public IReadOnlyCollection<string> Flags { get; init; } = [];

    /// <summary>
    /// Directory to write the printed files.
    /// Null or blank means the generator's default location.
    /// A relative path is resolved against the working directory.
    /// </summary>
    public string? OutputDir { get; init; }

    public static BindingPrintOptions Default { get; } = new();

    public bool Has(string flagName) => this.Flags.Contains(flagName, StringComparer.OrdinalIgnoreCase);
}

public class BindingPrintResult
{
    public int ActionCount { get; init; }

    public IReadOnlyList<string> WrittenPaths { get; init; } = [];
}

/// <summary>
/// Optional capability for a game manager that can print human-readable binding docs.
/// The CLI registers a <c>print</c> command for managers that implement this, and builds its
/// subcommands and options from <see cref="PrintFormats"/> and <see cref="PrintFlags"/>.
/// </summary>
public interface IBindingDocsGenerator
{
    /// <summary>The individual outputs that can be printed. Printing without choosing one prints all of them.</summary>
    IReadOnlyList<BindingPrintFormat> PrintFormats { get; }

    /// <summary>The extra switches the print command accepts.</summary>
    IReadOnlyList<BindingPrintFlag> PrintFlags { get; }

    /// <param name="format">The <see cref="BindingPrintFormat.Name"/> to print, or null for all formats.</param>
    Task<BindingPrintResult> Print(string? format, BindingPrintOptions printOptions, Dictionary<string, string>? options = null, CancellationToken cancellationToken = default);
}
