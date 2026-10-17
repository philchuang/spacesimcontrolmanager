namespace SSCM.Core;

/// <summary>
/// Finds a file that ships with the tool but that users can override.
/// <list type="number">
/// <item>The primary directory (the working directory) is checked first, so a file placed there wins.</item>
/// <item>The fallback directory (the executable directory, where the build copies the shipped file) is checked second.</item>
/// </list>
/// Anything the tool writes goes to <see cref="Path"/>, so it never needs write access to the install folder.
/// </summary>
public class ResourceFile
{
    public string FileName { get; }

    /// <summary>The first place checked, and where new copies are written.</summary>
    public string Path { get; }

    /// <summary>The second place checked. Null when there is no fallback, or when it is the same file as <see cref="Path"/>.</summary>
    public string? FallbackPath { get; }

    /// <summary>The locations checked, for messages: <c>[a]</c>, or <c>[a] or [b]</c>.</summary>
    public string Searched => this.FallbackPath == null ? $"[{this.Path}]" : $"[{this.Path}] or [{this.FallbackPath}]";

    public ResourceFile(string fileName, string directory, string? fallbackDirectory = null)
    {
        this.FileName = fileName;
        this.Path = System.IO.Path.Combine(directory, fileName);

        var fallback = fallbackDirectory == null ? null : System.IO.Path.Combine(fallbackDirectory, fileName);
        this.FallbackPath = fallback != null && !SamePath(fallback, this.Path) ? fallback : null;
    }

    /// <summary>The standard locations: the working directory first, then the executable directory.</summary>
    /// <param name="executableDir">The fallback location. Defaults to the executable directory.</param>
    public static ResourceFile Create(IPlatform platform, string fileName, string? executableDir = null) =>
        new(fileName, platform.WorkingDir, executableDir ?? AppContext.BaseDirectory);

    /// <summary>The first location that has the file, or null when neither does.</summary>
    public string? FindExistingPath()
    {
        if (File.Exists(this.Path)) return this.Path;
        if (this.FallbackPath != null && File.Exists(this.FallbackPath)) return this.FallbackPath;
        return null;
    }

    /// <summary>Like <see cref="FindExistingPath"/>, but throws a <see cref="FileNotFoundException"/> naming every location checked.</summary>
    /// <param name="hint">Optional advice appended to the message, such as how to create the file.</param>
    public string Require(string? hint = null)
    {
        var path = this.FindExistingPath();
        if (path != null) return path;

        var message = $"Could not find {this.FileName} at {this.Searched}.";
        if (!string.IsNullOrEmpty(hint)) message += " " + hint;
        throw new FileNotFoundException(message, this.Path);
    }

    private static bool SamePath(string a, string b) =>
        string.Equals(System.IO.Path.GetFullPath(a), System.IO.Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
}
