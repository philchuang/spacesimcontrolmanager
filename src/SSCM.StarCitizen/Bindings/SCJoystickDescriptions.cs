using System.Text.RegularExpressions;
using SSCM.Core;

namespace SSCM.StarCitizen.Bindings;

/// <summary>
/// Maps Star Citizen joystick inputs (such as <c>js1_button3</c>) to friendly names
/// from <see cref="JoystickDescriptions"/>.
/// The mapping is best-effort: devices that are not in actionmaps.xml are warned about, and a joystick
/// input without a name is described as <see cref="JoystickDescriptions.NotFound"/>.
/// </summary>
public static partial class SCJoystickDescriptions
{
    public const string Game = "star_citizen";
    public const string NotFound = JoystickDescriptions.NotFound;

    [GeneratedRegex(@"\s+\d+x$")]
    private static partial Regex MultiTapSuffixRegex();

    /// <summary>Reads each joystick's Product string and instance from imported input devices as product to <c>js{instance}</c>.</summary>
    public static Dictionary<string, string> LoadJoystickInstances(IEnumerable<SCInputDevice> inputs)
    {
        var instances = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var input in inputs)
        {
            if (!input.Type.Equals("joystick", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(input.Product) || input.Instance < 0) continue;

            var joystickId = $"js{input.Instance}";
            if (instances.TryGetValue(input.Product, out var previous) && previous != joystickId)
            {
                throw new InvalidDataException($"Product '{input.Product}' is both {previous} and {joystickId}.");
            }

            instances[input.Product] = joystickId;
        }

        return instances;
    }

    public static void WarnUnattached(JoystickDescriptions descriptions, IReadOnlyDictionary<string, string> joystickInstances, Action<string>? onWarning)
    {
        var known = descriptions.DeviceIds(Game).ToHashSet(StringComparer.Ordinal);
        foreach (var deviceId in known)
        {
            if (joystickInstances.ContainsKey(deviceId)) continue;
            onWarning?.Invoke($"Joystick device '{deviceId}' from joystick-bindings.md is not in actionmaps.xml. Its bindings are skipped.");
        }

        // An empty file already produced its own warning. Only compare hardware when the file named devices.
        if (known.Count == 0) return;
        foreach (var product in joystickInstances.Keys)
        {
            if (known.Contains(product)) continue;
            onWarning?.Invoke($"Joystick '{product}' is in actionmaps.xml but not in joystick-bindings.md. Its inputs will be {NotFound}. Copy joystick-bindings.md to the current directory and edit it to match your hardware.");
        }
    }

    /// <summary>
    /// Sets <see cref="BindingAction.JoystickDescription"/> for actions that have a joystick rebind (<c>js#_...</c>),
    /// ignoring a trailing multi-tap suffix such as " 2x". A rebind without a name is <see cref="JoystickDescriptions.NotFound"/>.
    /// Anything else (no binding, <c>{UNBOUND}</c>, or a parenthesized game default) has nothing to describe and gets an empty description.
    /// </summary>
    public static void Apply(IEnumerable<BindingAction> actions, JoystickDescriptions descriptions, IReadOnlyDictionary<string, string> joystickInstances)
    {
        var productByInstance = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (product, instance) in joystickInstances)
        {
            productByInstance[instance] = product;
        }

        foreach (var action in actions)
        {
            if (!action.Joystick.StartsWith("js", StringComparison.Ordinal))
            {
                action.JoystickDescription = string.Empty;
                continue;
            }

            var input = MultiTapSuffixRegex().Replace(action.Joystick, string.Empty);
            var split = input.IndexOf('_');
            if (split < 0
                || !productByInstance.TryGetValue(input[..split], out var product))
            {
                action.JoystickDescription = NotFound;
                continue;
            }

            action.JoystickDescription = descriptions.Get(Game, product).TryGetValue(input[(split + 1)..], out var name) ? name : NotFound;
        }
    }
}
