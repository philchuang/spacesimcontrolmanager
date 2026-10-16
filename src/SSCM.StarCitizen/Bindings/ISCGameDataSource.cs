using System.Xml.Linq;

namespace SSCM.StarCitizen.Bindings;

/// <summary>What <see cref="ISCGameDataSource"/> pulled out of the game files.</summary>
/// <param name="DefaultProfile">The game's default input profile.</param>
/// <param name="Localization">The raw bytes of the English <c>global.ini</c>. Null when it was not requested, or the game files do not have it.</param>
public sealed record SCGameData(XDocument DefaultProfile, byte[]? Localization);

/// <summary>Extracts the files this tool needs from the selected environment's game archive.</summary>
public interface ISCGameDataSource
{
    /// <param name="includeLocalization">Also extract <c>global.ini</c>. It comes out of the same pass over the archive as the profile.</param>
    Task<SCGameData> Extract(bool includeLocalization, CancellationToken cancellationToken = default);
}
