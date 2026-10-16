using System.Text.RegularExpressions;

namespace SSCM.Elite;

/// <summary>
/// Finds the user's Custom binds file in an Elite Dangerous Bindings directory.
/// Files are named <c>Custom.{major}.{minor}.binds</c>. When several exist, the highest
/// major.minor version is chosen (4.2 beats 4.1, 4.0, and 3.0).
/// </summary>
public sealed partial class CustomBindsFile
{
    public const string FileNameFormat = "Custom.{major}.{minor}.binds";

    public string Directory { get; }

    /// <summary>The location checked, for messages: <c>[dir]</c>.</summary>
    public string Searched => $"[{this.Directory}]";

    public CustomBindsFile(string directory)
    {
        this.Directory = directory;
    }

    /// <summary>The highest-version Custom binds file in the directory, or null when none exist (or the directory is missing).</summary>
    public string? FindExistingPath() => this.Matches().FirstOrDefault()?.Path;

    /// <summary>Like <see cref="FindExistingPath"/>, but throws a <see cref="FileNotFoundException"/> naming the directory.</summary>
    /// <param name="hint">Optional advice appended to the message, such as how to create the file.</param>
    public string Require(string? hint = null)
    {
        var path = this.FindExistingPath();
        if (path != null) return path;

        var message = $"Could not find a {FileNameFormat} file in {this.Searched}.";
        if (!string.IsNullOrEmpty(hint)) message += " " + hint;
        throw new FileNotFoundException(message, this.Directory);
    }

    private IEnumerable<Match> Matches()
    {
        if (!System.IO.Directory.Exists(this.Directory)) return [];

        return System.IO.Directory.EnumerateFiles(this.Directory)
            .Select(TryParse)
            .Where(match => match != null)
            .Select(match => match!)
            .OrderByDescending(match => match.Major)
            .ThenByDescending(match => match.Minor);
    }

    private static Match? TryParse(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        var captured = FileNameRegex().Match(name);
        if (!captured.Success) return null;

        return new Match(int.Parse(captured.Groups[1].Value), int.Parse(captured.Groups[2].Value), path);
    }

    [GeneratedRegex(@"^Custom\.(\d+)\.(\d+)\.binds$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FileNameRegex();

    private sealed record Match(int Major, int Minor, string Path);
}
