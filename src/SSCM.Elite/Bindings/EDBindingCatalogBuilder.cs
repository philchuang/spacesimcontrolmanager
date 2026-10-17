using SSCM.Core;

namespace SSCM.Elite.Bindings;

/// <summary>
/// Builds the flat action catalog from the user's Custom binds, an optional default ControlSchemes
/// file, and optional localization.
/// </summary>
public static class EDBindingCatalogBuilder
{
    public const string SlotSeparator = "; ";

    /// <summary>Appended to an action name when a localization file exists but has no entry for it.</summary>
    public const string KeyNotFoundSuffix = "/KNF";

    public static List<BindingAction> Build(
        EDMappingData custom,
        EDMappingData? defaults = null,
        IReadOnlyDictionary<string, string>? localization = null)
    {
        var customByName = Index(custom);
        if (defaults == null)
        {
            return custom.Mappings
                .Select(mapping => FromMapping(mapping, localization))
                .OrderBy(action => action.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var defaultsByName = Index(defaults);
        var names = customByName.Keys
            .Concat(defaultsByName.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var actions = new List<BindingAction>();
        foreach (var name in names)
        {
            customByName.TryGetValue(name, out var customMapping);
            defaultsByName.TryGetValue(name, out var defaultMapping);
            var identity = customMapping ?? defaultMapping!;
            var foldedDefaults = defaultMapping == null ? (Keyboard: "", Joystick: "") : Fold(defaultMapping);

            var action = new BindingAction
            {
                Id = identity.Id,
                CategoryName = identity.Group,
                CategoryLabel = identity.Group,
                Name = identity.Name,
                Label = Localize(identity.Name, localization),
                DefaultKeyboard = foldedDefaults.Keyboard,
                DefaultKeyboardSource = defaultMapping == null ? "keyboard" : KeyboardSource(defaultMapping),
                DefaultJoystick = foldedDefaults.Joystick,
            };

            if (customMapping != null)
            {
                var current = Fold(customMapping);
                action.Keyboard = current.Keyboard;
                action.Joystick = current.Joystick;
            }
            else
            {
                action.Keyboard = CopyDefaultBinding(foldedDefaults.Keyboard);
                action.Joystick = CopyDefaultBinding(foldedDefaults.Joystick);
            }

            actions.Add(action);
        }

        return actions.OrderBy(action => action.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static bool IsKeyboardOrMouse(string device) =>
        device.Equals("Keyboard", StringComparison.OrdinalIgnoreCase)
        || device.Equals("Mouse", StringComparison.OrdinalIgnoreCase);

    internal static (string Keyboard, string Joystick) Fold(EDMapping mapping)
    {
        var usedDevices = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keyboards = new List<string>();
        var joysticks = new List<string>();

        foreach (var slot in Slots(mapping))
        {
            if (slot == null || slot.IsUnbound) continue;
            if (!usedDevices.Add(slot.Key.Device)) continue;

            var text = slot.ToString();
            if (IsKeyboardOrMouse(slot.Key.Device)) keyboards.Add(text);
            else joysticks.Add(text);
        }

        return (string.Join(SlotSeparator, keyboards), string.Join(SlotSeparator, joysticks));
    }

    private static BindingAction FromMapping(EDMapping mapping, IReadOnlyDictionary<string, string>? localization)
    {
        var folded = Fold(mapping);
        return new BindingAction
        {
            Id = mapping.Id,
            CategoryName = mapping.Group,
            CategoryLabel = mapping.Group,
            Name = mapping.Name,
            Label = Localize(mapping.Name, localization),
            DefaultKeyboardSource = "keyboard",
            Keyboard = folded.Keyboard,
            Joystick = folded.Joystick,
        };
    }

    private static Dictionary<string, EDMapping> Index(EDMappingData? data) =>
        data?.Mappings.ToDictionary(mapping => mapping.Name, StringComparer.OrdinalIgnoreCase)
        ?? new Dictionary<string, EDMapping>(StringComparer.OrdinalIgnoreCase);

    private static string KeyboardSource(EDMapping mapping)
    {
        foreach (var slot in Slots(mapping))
        {
            if (slot == null || slot.IsUnbound) continue;
            if (slot.Key.Device.Equals("Mouse", StringComparison.OrdinalIgnoreCase)) return "mouse";
        }

        return "keyboard";
    }

    private static IEnumerable<EDBinding?> Slots(EDMapping mapping)
    {
        yield return mapping.Binding;
        yield return mapping.Primary;
        yield return mapping.Secondary;
    }

    private static string CopyDefaultBinding(string value) =>
        value.Length > 0 ? $"({value})" : string.Empty;

    // TODO: lookup key format once Elite localization files are known. Today the XML action name is used as the key.
    private static string Localize(string name, IReadOnlyDictionary<string, string>? localization)
    {
        if (localization == null) return name;
        return localization.TryGetValue(name, out var label) ? label : name + KeyNotFoundSuffix;
    }
}
