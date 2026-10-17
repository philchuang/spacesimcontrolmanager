using System.Globalization;
using System.Text;

namespace SSCM.Core;

/// <summary>Shared print formats, file names, and write helpers used by each game's binding-docs printer.</summary>
public static class BindingPrintIO
{
    public const string JoystickBindingsFileName = "joystick-bindings.md";
    public const string MarkdownFileName = "bindings.md";
    public const string TsvFileName = "bindings.tsv";
    public const string HtmlFileName = "bindings.html";
    public const string JsonFileName = "bindings.json";

    public static readonly BindingPrintFormat MarkdownFormat = new("markdown", "Prints only the bindings markdown table.");
    public static readonly BindingPrintFormat TsvFormat = new("tsv", "Prints only the bindings TSV.");
    public static readonly BindingPrintFormat HtmlFormat = new("html", "Prints only the interactive bindings HTML page, along with its bindings.json data.");

    public static IReadOnlyList<BindingPrintFormat> Formats { get; } = [MarkdownFormat, TsvFormat, HtmlFormat];

    /// <summary>Null (or blank) means every format. A name that isn't one of <paramref name="formats"/> is a user error, so it gets a friendly message.</summary>
    public static BindingPrintFormat? ResolveFormat(string? name, IReadOnlyList<BindingPrintFormat> formats)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var format = formats.FirstOrDefault(f => f.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        return format ?? throw new SscmException($"Unknown print format [{name}]. Available formats: {string.Join(", ", formats.Select(f => f.Name))}.");
    }

    /// <summary>Uses <paramref name="defaultOutputDir"/> when no directory was given. A relative path is resolved against the working directory.</summary>
    public static string ResolveOutputDir(IPlatform platform, string defaultOutputDir, BindingPrintOptions? printOptions)
    {
        if (string.IsNullOrWhiteSpace(printOptions?.OutputDir)) return defaultOutputDir;

        var specified = printOptions.OutputDir.Trim();
        return Path.IsPathRooted(specified)
            ? Path.GetFullPath(specified)
            : Path.GetFullPath(Path.Combine(platform.WorkingDir, specified));
    }

    public static string CurrentTimestamp(IPlatform platform) =>
        new DateTimeOffset(DateTime.SpecifyKind(platform.UtcNow, DateTimeKind.Utc))
            .ToLocalTime()
            .ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);

    public static string Write(string outputDir, string fileName, string content, string description, Action<string> standardOutput)
    {
        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, fileName);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        standardOutput($"Wrote {description} to {path}");
        return path;
    }
}
