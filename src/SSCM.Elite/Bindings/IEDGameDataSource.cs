using System.Xml.Linq;

namespace SSCM.Elite.Bindings;

/// <summary>
/// Localization (and any other install data that is not the ControlSchemes bind files) extracted from the Elite Dangerous install.
/// </summary>
public sealed record EDGameData(XDocument? DefaultBinds, byte[]? Localization);

/// <summary>
/// Where --refresh-defaults would extract localization from the game.
/// Default binds are read from the configured ControlSchemes file; this source is the placeholder for localization.
/// </summary>
public interface IEDGameDataSource
{
    Task<EDGameData> Extract(bool includeLocalization, CancellationToken cancellationToken = default);
}

/// <summary>
/// Placeholder until Elite localization can be extracted from the game files.
/// </summary>
public sealed class UnimplementedEDGameDataSource : IEDGameDataSource
{
    public const string Message = "Elite localization extraction is not implemented yet.";

    public Task<EDGameData> Extract(bool includeLocalization, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException(Message);
}
