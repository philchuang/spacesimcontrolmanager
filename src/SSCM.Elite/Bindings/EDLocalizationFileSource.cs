using System.Text;
using SSCM.Core;

namespace SSCM.Elite.Bindings;

/// <summary>
/// Reads an optional <c>elite-localization.ini</c> file (working directory first, then the executable directory).
/// The on-disk name and <c>key=value</c> format may change once the game source for localization is known.
/// </summary>
public class EDLocalizationFileSource
{
    public const string FileName = "elite-localization.ini";

    private readonly ResourceFile _file;

    public static EDLocalizationFileSource Create(IPlatform platform, string? executableDir = null) =>
        new(platform.WorkingDir, executableDir ?? AppContext.BaseDirectory);

    public string Path => this._file.Path;
    public string? FallbackPath => this._file.FallbackPath;
    public string Searched => this._file.Searched;

    public EDLocalizationFileSource(string directory, string? fallbackDirectory = null)
    {
        this._file = new ResourceFile(FileName, directory, fallbackDirectory);
    }

    public string? FindExistingPath() => this._file.FindExistingPath();

    public async Task<Dictionary<string, string>?> Load(CancellationToken cancellationToken = default)
    {
        var path = this.FindExistingPath();
        if (path == null) return null;

        return ParseLocalization(await File.ReadAllLinesAsync(path, Encoding.UTF8, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Parses ini lines: first '=' splits key and value; blanks, ';' comments, and lines without '=' are skipped.</summary>
    public static Dictionary<string, string> ParseLocalization(IEnumerable<string> lines)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            if (line.Length == 0 || line.TrimStart().StartsWith(';')) continue;

            var separator = line.IndexOf('=');
            if (separator < 0) continue;

            map[line[..separator]] = line[(separator + 1)..];
        }

        return map;
    }
}
