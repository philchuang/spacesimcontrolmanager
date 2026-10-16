using System.Text.RegularExpressions;
using System.Xml.Linq;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SSCM.StarCitizen.Bindings;

/// <summary>
/// Maps Star Citizen joystick inputs (such as <c>js1_button3</c>) to friendly names
/// defined in the YAML block of joystick-bindings.md.
/// The mapping is best-effort: devices that are not in actionmaps.xml are skipped, and a joystick
/// input without a name is described as <see cref="NotFound"/>.
/// </summary>
public static partial class JoystickDescriptions
{
    /// <summary>The description given to a bound joystick input that has no friendly name.</summary>
    public const string NotFound = "{NOT_FOUND}";

    private static readonly IDeserializer Yaml = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    [GeneratedRegex(@"\s+\d+x$")]
    private static partial Regex MultiTapSuffixRegex();

    /// <summary>Reads each joystick's Product string and instance from actionmaps.xml as product to <c>js{instance}</c>.</summary>
    public static Dictionary<string, string> LoadJoystickInstances(XDocument actionMaps)
    {
        var instances = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var options in actionMaps.Root?.Descendants("options") ?? [])
        {
            if ((string?) options.Attribute("type") != "joystick") continue;

            var product = (string?) options.Attribute("Product") ?? string.Empty;
            var instance = (string?) options.Attribute("instance") ?? string.Empty;
            if (product.Length == 0 || instance.Length == 0) continue;

            var joystickId = $"js{instance}";
            if (instances.TryGetValue(product, out var previous) && previous != joystickId)
            {
                throw new InvalidDataException($"Product '{product}' is both {previous} and {joystickId}.");
            }

            instances[product] = joystickId;
        }

        return instances;
    }

    /// <summary>
    /// Parses the YAML bindings block into <c>js#_input</c> to friendly name.
    /// A device whose <c>star_citizen_device_id</c> is not in <paramref name="joystickInstances"/> is skipped with a warning,
    /// since the file describes hardware that may not be attached to this installation.
    /// </summary>
    public static Dictionary<string, string> LoadNames(string markdown, IReadOnlyDictionary<string, string> joystickInstances, Action<string>? onWarning = null)
    {
        var devices = ParseDevices(ExtractYamlBlock(markdown));
        var names = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (deviceName, device) in devices)
        {
            var product = device.StarCitizenDeviceId;
            if (product.Length == 0 || !joystickInstances.TryGetValue(product, out var joystickId))
            {
                if (product.Length == 0 && device.Bindings.Values.Any(binding => binding.StarCitizen.Length > 0))
                {
                    throw new InvalidDataException($"star_citizen without a joystick or button name: {deviceName}");
                }

                if (product.Length > 0)
                {
                    onWarning?.Invoke($"Joystick device '{product}' from joystick-bindings.md is not in actionmaps.xml. Its bindings are skipped.");
                }

                continue;
            }

            foreach (var (bindingName, binding) in device.Bindings)
            {
                if (binding.StarCitizen.Length == 0) continue;

                var inputName = $"{joystickId}_{binding.StarCitizen}";
                if (names.TryGetValue(inputName, out var previous) && previous != bindingName)
                {
                    throw new InvalidDataException($"Duplicate Star Citizen input {inputName}: {previous} and {bindingName}.");
                }

                names[inputName] = bindingName;
            }
        }

        return names;
    }

    private static string ExtractYamlBlock(string markdown)
    {
        var text = markdown.Replace("\r\n", "\n").Replace('\r', '\n');
        var search = 0;
        while (search < text.Length)
        {
            var start = text.IndexOf("```yaml", search, StringComparison.Ordinal);
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
            var close = text.IndexOf("\n```", contentStart, StringComparison.Ordinal);
            if (close < 0) break;
            return text[contentStart..close];
        }

        throw new InvalidDataException("No yaml bindings block was found in joystick-bindings.md.");
    }

    private static Dictionary<string, DeviceDescription> ParseDevices(string yaml)
    {
        try
        {
            return Yaml.Deserialize<Dictionary<string, DeviceDescription>>(yaml) ?? new Dictionary<string, DeviceDescription>();
        }
        catch (YamlException ex)
        {
            throw new InvalidDataException("The yaml bindings block in joystick-bindings.md could not be parsed.", ex);
        }
    }

    private sealed class DeviceDescription
    {
        public string StarCitizenDeviceId { get; set; } = string.Empty;
        public Dictionary<string, InputDescription> Bindings { get; set; } = new();
    }

    private sealed class InputDescription
    {
        public string StarCitizen { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sets <see cref="BindingAction.JoystickDescription"/> for actions that have a joystick rebind (<c>js#_...</c>),
    /// ignoring a trailing multi-tap suffix such as " 2x". A rebind without a name is <see cref="NotFound"/>.
    /// Anything else (no binding, <c>{UNBOUND}</c>, or a parenthesized game default) has nothing to describe and gets an empty description.
    /// </summary>
    public static void Apply(IEnumerable<BindingAction> actions, IReadOnlyDictionary<string, string> names)
    {
        foreach (var action in actions)
        {
            if (!action.Joystick.StartsWith("js", StringComparison.Ordinal))
            {
                action.JoystickDescription = string.Empty;
                continue;
            }

            var input = MultiTapSuffixRegex().Replace(action.Joystick, string.Empty);
            action.JoystickDescription = names.TryGetValue(input, out var name) ? name : NotFound;
        }
    }
}
