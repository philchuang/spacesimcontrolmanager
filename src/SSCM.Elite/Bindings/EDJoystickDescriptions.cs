using SSCM.Core;

namespace SSCM.Elite.Bindings;

/// <summary>
/// Maps Elite Dangerous joystick inputs (such as <c>231D3205-Joy_4</c>) to friendly names
/// from <see cref="JoystickDescriptions"/>.
/// Devices that are not in the current binds file are warned about, and a bound input without a name
/// is described as <see cref="JoystickDescriptions.NotFound"/>.
/// </summary>
public static class EDJoystickDescriptions
{
    public const string Game = "elite_dangerous";
    public const string NotFound = JoystickDescriptions.NotFound;

    public static HashSet<string> LoadAttachedDeviceIds(EDMappingData data)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mapping in data.Mappings)
        {
            Add(ids, mapping.Binding);
            Add(ids, mapping.Primary);
            Add(ids, mapping.Secondary);
        }

        return ids;
    }

    public static void WarnUnattached(JoystickDescriptions descriptions, IReadOnlySet<string> attachedDeviceIds, Action<string>? onWarning)
    {
        foreach (var deviceId in descriptions.DeviceIds(Game))
        {
            if (attachedDeviceIds.Contains(deviceId)) continue;
            onWarning?.Invoke($"Joystick device '{deviceId}' from joystick-bindings.md is not in the Elite binds file. Its bindings are skipped.");
        }
    }

    /// <summary>
    /// Sets <see cref="BindingAction.JoystickDescription"/> for actions that have a joystick bind.
    /// Several slots joined with <see cref="EDBindingCatalogBuilder.SlotSeparator"/> are described in the same order.
    /// Modifier combinations joined with <c> + </c> describe every involved input, not just the primary button.
    /// A parenthesized game default has nothing to describe and gets an empty description.
    /// </summary>
    public static void Apply(IEnumerable<BindingAction> actions, JoystickDescriptions descriptions)
    {
        foreach (var action in actions)
        {
            if (string.IsNullOrEmpty(action.Joystick) || action.Joystick.StartsWith('('))
            {
                action.JoystickDescription = string.Empty;
                continue;
            }

            var slots = action.Joystick.Split(EDBindingCatalogBuilder.SlotSeparator);
            action.JoystickDescription = string.Join(
                EDBindingCatalogBuilder.SlotSeparator,
                slots.Select(slot => Describe(slot, descriptions)));
        }
    }

    private static void Add(HashSet<string> ids, EDBinding? binding)
    {
        if (binding == null || binding.IsUnbound) return;
        AddDevice(ids, binding.Key.Device);
        foreach (var modifier in binding.Modifiers) AddDevice(ids, modifier.Device);
    }

    private static void AddDevice(HashSet<string> ids, string device)
    {
        if (device.Length == 0 || EDBindingCatalogBuilder.IsKeyboardOrMouse(device)) return;
        ids.Add(device);
    }

    private static string Describe(string slot, JoystickDescriptions descriptions) =>
        string.Join(" + ", slot.Split(" + ", StringSplitOptions.None).Select(part => DescribePart(part, descriptions)));

    private static string DescribePart(string part, JoystickDescriptions descriptions)
    {
        var dash = part.IndexOf('-');
        if (dash < 0) return NotFound;

        var device = part[..dash];
        var key = EDBindingDisplay.AxisLookupKey(part[(dash + 1)..]);
        if (EDBindingCatalogBuilder.IsKeyboardOrMouse(device))
        {
            return part.Replace("Keyboard-", "", StringComparison.Ordinal).Replace("Mouse-", "", StringComparison.Ordinal);
        }

        return descriptions.Get(Game, device).TryGetValue(key, out var name) ? name : NotFound;
    }
}
