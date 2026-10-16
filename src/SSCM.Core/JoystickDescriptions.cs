namespace SSCM.Core;

/// <summary>
/// Game-agnostic friendly names from joystick-bindings.md, keyed by game, device id, then that game's bind string.
/// </summary>
public sealed class JoystickDescriptions
{
    public const string NotFound = "{NOT_FOUND}";

    public static JoystickDescriptions Empty { get; } = new(new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.Ordinal));

    private static readonly IReadOnlyDictionary<string, string> NoBindings =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _games;

    internal JoystickDescriptions(Dictionary<string, Dictionary<string, Dictionary<string, string>>> games)
    {
        this._games = games;
    }

    /// <summary>game id → device id → game binding name → description.</summary>
    public IReadOnlyDictionary<string, Dictionary<string, Dictionary<string, string>>> Games => this._games;

    /// <summary>Binding name → description for one game device, or an empty map when that device is unknown.</summary>
    public IReadOnlyDictionary<string, string> Get(string gameId, string deviceId) =>
        this._games.TryGetValue(gameId, out var devices) && devices.TryGetValue(deviceId, out var bindings)
            ? bindings
            : NoBindings;

    public IEnumerable<string> DeviceIds(string gameId) =>
        this._games.TryGetValue(gameId, out var devices) ? devices.Keys : [];
}
