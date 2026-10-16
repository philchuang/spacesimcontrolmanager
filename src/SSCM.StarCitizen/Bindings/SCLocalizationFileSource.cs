using System.Text;
using SSCM.Core;

namespace SSCM.StarCitizen.Bindings;

/// <summary>
/// Reads and writes the English <c>global.ini</c> localization file. This is the one place that knows where the file lives.
/// <list type="bullet">
/// <item>Reading always uses the game's own copy under the selected environment when it exists, then falls back to
/// <c>global.ini</c> in the working directory (for example one extracted by <c>--refresh-defaults</c>).</item>
/// <item>Writing always goes to the working directory, and only when the game has no copy yet.</item>
/// </list>
/// </summary>
public class SCLocalizationFileSource
{
    public static SCLocalizationFileSource Create(IPlatform platform, ISCFolders folders) =>
        new(folders, platform.WorkingDir);

    private readonly ISCFolders _folders;

    /// <summary>Where <see cref="Save"/> writes when the game has no copy.</summary>
    public string WorkingPath { get; }

    /// <summary>The selected environment's own file (<c>...\LIVE\data\Localization\english\global.ini</c>). It is not checked for existence.</summary>
    public string GamePath => this._folders.GameLocalizationPath;

    /// <summary>The locations checked when reading, for messages: <c>[game] or [working]</c>.</summary>
    public string Searched => $"[{this.GamePath}] or [{this.WorkingPath}]";

    public SCLocalizationFileSource(ISCFolders folders, string workingDirectory)
    {
        this._folders = folders;
        this.WorkingPath = Path.Combine(workingDirectory, Constants.SC_GLOBAL_INI_NAME);
    }

    /// <summary>True when the game install already has its own global.ini.</summary>
    public bool GameFileExists => File.Exists(this.GamePath);

    /// <summary>True when <see cref="RefreshDefaults"/> should pull global.ini out of Data.p4k.</summary>
    public bool NeedsExtractionFromArchive => !this.GameFileExists;

    /// <summary>The file that would be read, or null when neither the game nor the working directory has one.</summary>
    public string? FindExistingPath()
    {
        if (this.GameFileExists) return this.GamePath;
        return File.Exists(this.WorkingPath) ? this.WorkingPath : null;
    }

    /// <summary>Parses the file that <see cref="FindExistingPath"/> would read. Null when there is no file.</summary>
    public async Task<Dictionary<string, string>?> Load(CancellationToken cancellationToken = default)
    {
        var path = this.FindExistingPath();
        if (path == null) return null;

        return ParseLocalization(await File.ReadAllLinesAsync(path, Encoding.UTF8, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Parses global.ini lines: first '=' splits key and value; blanks, ';' comments, and lines without '=' are skipped.</summary>
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

    /// <summary>Overwrites the working-directory copy. The write goes to a temp file first so a failure cannot leave a partial file behind.</summary>
    public void Save(byte[] content)
    {
        var directory = Path.GetDirectoryName(this.WorkingPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var temp = this.WorkingPath + ".tmp";
        try
        {
            File.WriteAllBytes(temp, content);
            File.Move(temp, this.WorkingPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }
}
