using System.Collections;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace SSCM.Core;

/// <summary>
/// Reads the YAML block out of joystick-bindings.md into <see cref="JoystickDescriptions"/>.
/// Device ids are keys ending in <c>_device_id</c> (for example <c>star_citizen_device_id</c>).
/// Binding values are keyed by the same game name (for example <c>star_citizen: button1</c>).
/// </summary>
public static class JoystickBindingsFile
{
    public const string BindingsKey = "bindings";
    public const string DeviceIdSuffix = "_device_id";

    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .Build();

    /// <summary>
    /// Loads joystick-bindings.md from the working directory, then the executable directory.
    /// The file is optional: a missing copy warns and returns <see cref="JoystickDescriptions.Empty"/>.
    /// </summary>
    public static async Task<JoystickDescriptions> Load(
        IPlatform platform,
        string? resourceDir = null,
        Action<string>? onWarning = null,
        Action<string>? onDebug = null,
        CancellationToken cancellationToken = default)
    {
        var file = ResourceFile.Create(platform, BindingPrintIO.JoystickBindingsFileName, resourceDir);
        var path = file.FindExistingPath();
        if (path == null)
        {
            onWarning?.Invoke($"Could not find {BindingPrintIO.JoystickBindingsFileName} at {file.Searched}. Joystick descriptions will be {JoystickDescriptions.NotFound}.");
            return JoystickDescriptions.Empty;
        }

        onDebug?.Invoke($"Reading joystick bindings [{path}]...");
        var markdown = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return Parse(markdown);
    }

    public static JoystickDescriptions Parse(string markdown)
    {
        // game id → device id → game bind string → friendly name, e.g. star_citizen / product / button1 → LT-EVO.01(FIRE)
        var games = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (deviceName, fields) in ParseDevices(ExtractYamlBlock(markdown)))
        {
            AddDevice(games, deviceName, fields);
        }

        return new JoystickDescriptions(games);
    }

    private static void AddDevice(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> games,
        string deviceName,
        IReadOnlyDictionary<string, object?> fields)
    {
        // From elite_dangerous_device_id / star_citizen_device_id: game id → that game's device id string.
        var deviceIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        // From bindings: friendly name → (game id → Joy_1 / button1 / …).
        var namedBindings = new List<(string Description, Dictionary<string, string> Inputs)>();

        foreach (var (key, value) in fields)
        {
            // bindings:
            //   "LT-EVO.01(FIRE)":
            //     elite_dangerous: Joy_1
            //     star_citizen: button1
            if (key.Equals(BindingsKey, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var (description, bindingValue) in Entries(value))
                {
                    namedBindings.Add((description, ToStringMap(bindingValue)));
                }

                continue;
            }

            // elite_dangerous_device_id: "231D3205"
            // star_citizen_device_id: " VKBsim Gladiator EVO …"
            if (key.EndsWith(DeviceIdSuffix, StringComparison.OrdinalIgnoreCase) && key.Length > DeviceIdSuffix.Length)
            {
                deviceIds[key[..^DeviceIdSuffix.Length]] = Scalar(value);
            }
        }

        // Invert friendly-name-first YAML into Get(game, deviceId)[bindingName] → description.
        foreach (var (description, inputs) in namedBindings)
        {
            foreach (var (gameId, bindingName) in inputs)
            {
                if (!deviceIds.TryGetValue(gameId, out var deviceId) || deviceId.Length == 0)
                {
                    throw new InvalidDataException($"{gameId} without a device id: {deviceName}");
                }

                var bindings = BindingsFor(games, gameId, deviceId);
                if (bindings.TryGetValue(bindingName, out var previous) && previous != description)
                {
                    throw new InvalidDataException($"Duplicate {gameId} input {deviceId} {bindingName}: {previous} and {description}.");
                }

                bindings[bindingName] = description;
            }
        }
    }

    private static Dictionary<string, string> BindingsFor(
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> games,
        string gameId,
        string deviceId)
    {
        if (!games.TryGetValue(gameId, out var devices))
        {
            devices = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            games[gameId] = devices;
        }

        if (!devices.TryGetValue(deviceId, out var bindings))
        {
            bindings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            devices[deviceId] = bindings;
        }

        return bindings;
    }

    /// <summary>The fenced <c>```yaml</c> block in joystick-bindings.md; prose above it is ignored.</summary>
    private static string ExtractYamlBlock(string markdown)
    {
        var text = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
        var search = 0;
        while (search < text.Length)
        {
            var start = text.IndexOf("```yaml", search, StringComparison.OrdinalIgnoreCase);
            if (start < 0) break;
            if (start > 0 && text[start - 1] != '\n')
            {
                search = start + "```yaml".Length;
                continue;
            }

            var lineEnd = text.IndexOf('\n', start);
            if (lineEnd < 0) break;
            if (text[(start + "```yaml".Length)..lineEnd].Trim().Length > 0)
            {
                search = lineEnd + 1;
                continue;
            }

            var contentStart = lineEnd + 1;
            var close = text.IndexOf("\n```", contentStart, StringComparison.OrdinalIgnoreCase);
            if (close < 0) break;
            return text[contentStart..close];
        }

        throw new InvalidDataException("No yaml bindings block was found in joystick-bindings.md.");
    }

    /// <summary>Top-level YAML keys are device names such as <c>LT-EVO</c>; each value is that device's fields.</summary>
    private static Dictionary<string, Dictionary<string, object?>> ParseDevices(string yaml)
    {
        try
        {
            var parsed = Yaml.Deserialize<Dictionary<string, Dictionary<string, object?>>>(yaml);
            return parsed ?? new Dictionary<string, Dictionary<string, object?>>();
        }
        catch (YamlException ex)
        {
            throw new InvalidDataException("The yaml bindings block in joystick-bindings.md could not be parsed.", ex);
        }
    }

    /// <summary>One binding's per-game inputs, e.g. <c>elite_dangerous: Joy_1</c> and <c>star_citizen: button1</c>.</summary>
    private static Dictionary<string, string> ToStringMap(object? value)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, nested) in Entries(value))
        {
            var text = Scalar(nested);
            if (text.Length > 0) map[key] = text;
        }

        return map;
    }

    private static IEnumerable<(string Key, object? Value)> Entries(object? value)
    {
        switch (value)
        {
            case Dictionary<string, object?> typed:
                foreach (var pair in typed) yield return (pair.Key, pair.Value);
                yield break;
            case Dictionary<object, object> untyped:
                foreach (var pair in untyped)
                {
                    var key = pair.Key?.ToString();
                    if (key != null) yield return (key, pair.Value);
                }

                yield break;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    var key = entry.Key?.ToString();
                    if (key != null) yield return (key, entry.Value);
                }

                yield break;
        }
    }

    private static string Scalar(object? value) =>
        value is null or IDictionary ? string.Empty : Convert.ToString(value) ?? string.Empty;
}
