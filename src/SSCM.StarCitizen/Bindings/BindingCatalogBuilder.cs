using System.Xml.Linq;

namespace SSCM.StarCitizen.Bindings;

/// <summary>
/// Builds the flat action catalog:
/// default profile actions, localized labels, and the user's rebinds from actionmaps.xml.
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
    public static List<BindingAction> Build(
        XDocument defaultProfile,
        IReadOnlyDictionary<string, string>? localization,
        XDocument actionMaps)
    {
        var rows = ReadDefaultActions(defaultProfile);

        foreach (var row in rows.Values)
        {
            row.Action.Label = Localize(row.Action.Label, localization);
            row.Action.CategoryLabel = Localize(row.Action.CategoryLabel, localization);
        }

        ApplyRebinds(rows, actionMaps);

        foreach (var row in rows.Values)
        {
            row.Action.Keyboard = row.Keyboard ?? CopyDefaultBinding(row.Action.DefaultKeyboard);
            row.Action.Joystick = row.Joystick ?? CopyDefaultBinding(row.Action.DefaultJoystick);
        }

        return rows.Values.Select(r => r.Action).OrderBy(a => a.Id, StringComparer.Ordinal).ToList();
    }

    private sealed class Row
    {
        public required BindingAction Action { get; init; }
        public string? Keyboard { get; set; }
        public string? Joystick { get; set; }
    }

    private static Dictionary<string, Row> ReadDefaultActions(XDocument defaultProfile)
    {
        var rows = new Dictionary<string, Row>(StringComparer.Ordinal);

        foreach (var actionMap in defaultProfile.Root?.Descendants("actionmap") ?? [])
        {
            var categoryName = Attr(actionMap, "name");
            var categoryLabel = Attr(actionMap, "UILabel");

            foreach (var action in actionMap.Elements("action"))
            {
                var actionName = Attr(action, "name");
                var rawKeyboard = Attr(action, "keyboard");

                string defaultKeyboard, defaultKeyboardSource;
                if (string.IsNullOrWhiteSpace(rawKeyboard))
                {
                    defaultKeyboard = Attr(action, "mouse");
                    defaultKeyboardSource = defaultKeyboard.Length > 0 ? "mouse" : "keyboard";
                }
                else
                {
                    defaultKeyboard = rawKeyboard;
                    defaultKeyboardSource = "keyboard";
                }

                var id = $"{categoryName}-{actionName}";
                rows[id] = new Row
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

        return rows;
    }

    private static void ApplyRebinds(Dictionary<string, Row> rows, XDocument actionMaps)
    {
        foreach (var actionMap in actionMaps.Root?.Descendants("actionmap") ?? [])
        {
            var categoryName = Attr(actionMap, "name");
            foreach (var action in actionMap.Elements("action"))
            {
                if (!rows.TryGetValue($"{categoryName}-{Attr(action, "name")}", out var row)) continue;

                foreach (var rebind in action.Elements("rebind"))
                {
                    var rawInput = Attr(rebind, "input");
                    var input = ApplyMultiTap(NormalizeInput(rawInput), Attr(rebind, "multiTap"));

                    if (rawInput.StartsWith("kb", StringComparison.Ordinal)) row.Keyboard ??= input;
                    else if (rawInput.StartsWith("js", StringComparison.Ordinal)) row.Joystick ??= input;
                }
            }
        }
    }

    private static string NormalizeInput(string input) => input.EndsWith(' ') ? Unbound : input;

    private static string ApplyMultiTap(string input, string multiTap)
    {
        var trimmed = multiTap.Trim();
        if (input.Length == 0 || trimmed.Length == 0 || input == Unbound) return input;
        return $"{input} {trimmed}x";
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
