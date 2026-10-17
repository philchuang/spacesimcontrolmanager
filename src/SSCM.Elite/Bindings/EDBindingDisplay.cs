using SSCM.Core;

namespace SSCM.Elite.Bindings;

/// <summary>
/// Elite display style: drop the redundant <c>Keyboard-</c> and <c>Mouse-</c> device prefixes
/// from bind fields, and show inverted axes (<c>Neg_Joy_*</c>) with a <c> (inv)</c> suffix.
/// </summary>
public static class EDBindingDisplay
{
    public const string InvertPrefix = "Neg_";
    public const string InvertSuffix = " (inv)";

    public static void Prepare(BindingDocument document)
    {
        foreach (var action in document.Actions)
        {
            action.DefaultKeyboard = StripDevicePrefixes(action.DefaultKeyboard);
            action.Keyboard = StripDevicePrefixes(action.Keyboard);
            action.JoystickDescription = StripDevicePrefixes(action.JoystickDescription);
            action.DefaultJoystick = FormatJoystick(StripDevicePrefixes(action.DefaultJoystick));
            action.Joystick = FormatJoystick(StripDevicePrefixes(action.Joystick));
        }
    }

    /// <summary>Elite inverted axes are stored as <c>Neg_Joy_*</c>; lookup uses the uninverted name.</summary>
    public static string AxisLookupKey(string key) =>
        key.StartsWith(InvertPrefix, StringComparison.Ordinal) ? key[InvertPrefix.Length..] : key;

    private static string StripDevicePrefixes(string value) =>
        value.Replace("Keyboard-", "", StringComparison.Ordinal).Replace("Mouse-", "", StringComparison.Ordinal);

    private static string FormatJoystick(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        return string.Join(
            EDBindingCatalogBuilder.SlotSeparator,
            value.Split(EDBindingCatalogBuilder.SlotSeparator).Select(FormatSlot));
    }

    private static string FormatSlot(string slot) =>
        string.Join(" + ", slot.Split(" + ", StringSplitOptions.None).Select(FormatPart));

    private static string FormatPart(string part)
    {
        var wrapped = part.Length >= 2 && part[0] == '(' && part[^1] == ')';
        var inner = wrapped ? part[1..^1] : part;
        var dash = inner.IndexOf('-');
        if (dash < 0)
        {
            return part;
        }

        var device = inner[..dash];
        var key = inner[(dash + 1)..];
        if (!key.StartsWith(InvertPrefix, StringComparison.Ordinal))
        {
            return part;
        }

        var formatted = $"{device}-{key[InvertPrefix.Length..]}{InvertSuffix}";
        return wrapped ? $"({formatted})" : formatted;
    }
}
