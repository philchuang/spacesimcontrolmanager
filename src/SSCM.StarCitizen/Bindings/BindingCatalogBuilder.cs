using System.Xml.Linq;
using SSCM.Core;

namespace SSCM.StarCitizen.Bindings;

/// <summary>
/// Builds the flat action catalog:
/// default profile actions, localized labels, and the user's rebinds from imported actionmaps data.
/// </summary>
public static class BindingCatalogBuilder
{
    private const string Unbound = "{UNBOUND}";

    /// <summary>Appended to a localization key when global.ini does not exist.</summary>
    public const string FileNotFoundSuffix = "/FNF";

    /// <summary>Appended to a localization key when global.ini exists but has no entry for it.</summary>
    public const string KeyNotFoundSuffix = "/KNF";

    /// <param name="localization">
    /// Key/value strings from global.ini, or null when that file does not exist.
    /// Only labels that are localization keys (starting with '@') are replaced; others are kept as is.
    /// </param>
    /// <param name="actionMaps">
    /// User rebinds and devices from <see cref="MappingImporter.ReadActionMaps"/>. Keyboard and mouse
    /// rebinds fill the keyboard column; joystick rebinds fill the joystick column. Gamepad is ignored.
    /// The first rebind of each column wins. Actions not in the default profile are dropped.
    /// </param>
    public static List<BindingAction> Build(
        XDocument defaultProfile,
        IReadOnlyDictionary<string, string>? localization,
        SCMappingData? actionMaps = null)
    {
        var defaultBinds = ReadDefaultActions(defaultProfile);

        foreach (var row in defaultBinds.Values)
        {
            row.Action.Label = Localize(row.Action.Label, localization);
            row.Action.CategoryLabel = Localize(row.Action.CategoryLabel, localization);
        }

        ApplyRebinds(defaultBinds, actionMaps);

        foreach (var row in defaultBinds.Values)
        {
            row.Action.Keyboard = row.Keyboard ?? CopyDefaultBinding(row.Action.DefaultKeyboard);
            row.Action.Joystick = row.Joystick ?? CopyDefaultBinding(row.Action.DefaultJoystick);
        }

        return defaultBinds.Values.Select(r => r.Action).OrderBy(a => a.Id, StringComparer.Ordinal).ToList();
    }

    private sealed class Row
    {
        public required BindingAction Action { get; init; }
        public string? Keyboard { get; set; }
        public string? Joystick { get; set; }
    }

    private static Dictionary<string, Row> ReadDefaultActions(XDocument defaultProfile)
    {
        var defaultBinds = new Dictionary<string, Row>(StringComparer.Ordinal);

        foreach (var actionMap in defaultProfile.Root?.Descendants("actionmap") ?? [])
        {
            var categoryName = Attr(actionMap, "name");
            var categoryLabel = Attr(actionMap, "UILabel");

            foreach (var action in actionMap.Elements("action"))
            {
                var actionName = Attr(action, "name");
                var rawKeyboard = Attr(action, "keyboard");
                var rawMouse = Attr(action, "mouse");

                // Mouse wins when both are set: the few dual defaults in defaultProfile.xml pair
                // a mouse bind with a nonsensical keyboard key.
                string defaultKeyboard, defaultKeyboardSource;
                if (!string.IsNullOrWhiteSpace(rawMouse))
                {
                    defaultKeyboard = rawMouse;
                    defaultKeyboardSource = "mouse";
                }
                else
                {
                    defaultKeyboard = rawKeyboard;
                    defaultKeyboardSource = "keyboard";
                }

                var id = $"{categoryName}-{actionName}";
                defaultBinds[id] = new Row
                {
                    Action = new BindingAction
                    {
                        Id = id,
                        CategoryName = categoryName,
                        CategoryLabel = categoryLabel,
                        Name = actionName,
                        Label = Attr(action, "UILabel"),
                        DefaultKeyboard = defaultKeyboard,
                        DefaultKeyboardSource = defaultKeyboardSource,
                        DefaultJoystick = Attr(action, "joystick"),
                    }
                };
            }
        }

        return defaultBinds;
    }

    private static void ApplyRebinds(Dictionary<string, Row> binds, SCMappingData? actionMaps)
    {
        if (actionMaps == null) return;

        foreach (var mapping in actionMaps.Mappings)
        {
            if (!binds.TryGetValue(mapping.Id, out var row)) continue;

            var input = FormatRebind(mapping);
            if (IsKeyboardOrMouse(mapping.InputType)) row.Keyboard ??= input;
            else if (mapping.InputType.Equals("joystick", StringComparison.OrdinalIgnoreCase)) row.Joystick ??= input;
        }
    }

    internal static bool IsKeyboardOrMouse(string inputType) =>
        inputType.Equals("keyboard", StringComparison.OrdinalIgnoreCase)
        || inputType.Equals("mouse", StringComparison.OrdinalIgnoreCase);

    private static string FormatRebind(SCMapping mapping)
    {
        if (mapping.Input.EndsWith(' ')) return Unbound;
        if (mapping.MultiTap is int tap && tap > 0) return $"{mapping.Input} {tap}x";
        return mapping.Input;
    }

    private static string CopyDefaultBinding(string value)
    {
        var normalized = value.EndsWith(' ') ? "UNBOUND" : value.Trim();
        return normalized.Length > 0 ? $"({normalized})" : string.Empty;
    }

    private static string Localize(string value, IReadOnlyDictionary<string, string>? localization)
    {
        if (value.Length == 0) return value;

        var isKey = value.StartsWith('@');
        if (localization == null) return isKey ? value + FileNotFoundSuffix : value;
        if (localization.TryGetValue(value, out var exact)) return exact;
        if (isKey && localization.TryGetValue(value[1..], out var withoutAt)) return withoutAt;
        return isKey ? value + KeyNotFoundSuffix : value;
    }

    private static string Attr(XElement element, string name) => (string?) element.Attribute(name) ?? string.Empty;
}
