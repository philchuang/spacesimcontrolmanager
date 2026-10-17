using System.Text;
using Newtonsoft.Json;

namespace SSCM.Core;

/// <summary>Renders a <see cref="BindingDocument"/> as markdown, TSV, or interactive HTML.</summary>
public static class BindingDocumentRenderer
{
    public const string BindingsJsonPlaceholder = "__BINDINGS_JSON__";

    private static readonly string[] Columns = ["id", "category", "description", "keyboard", "joystick", "joystickDesc"];

    public static string RenderMarkdown(BindingDocument document, string heading)
    {
        // Rows without a category or description are noise in markdown, so they are dropped.
        var rows = BuildRows(document, skipMissingCategoryOrDescription: true);

        var lines = new List<string>
        {
            $"# {heading}",
            "",
            "## GENERATED BINDINGS",
            "",
            "|" + string.Join("|", Columns) + "|",
            "|" + string.Join("|", Columns.Select(_ => "---")) + "|",
        };
        lines.AddRange(rows.Select(row => "|" + string.Join("|", row.Select(MarkdownEscape)) + "|"));
        return string.Join("\n", lines) + "\n";
    }

    public static string RenderTsv(BindingDocument document)
    {
        var rows = BuildRows(document, skipMissingCategoryOrDescription: false);

        var lines = new List<string> { string.Join("\t", Columns) };
        lines.AddRange(rows.Select(row => string.Join("\t", row.Select(TsvEscape))));
        return string.Join("\n", lines) + "\n";
    }

    /// <summary>The full document (every field, not just the displayed columns) as indented JSON with LF line endings.</summary>
    public static string RenderJson(BindingDocument document)
    {
        var json = new JsonBindingDocument
        {
            GeneratedAt = document.GeneratedAt,
            Actions = document.Actions.Select(a => new JsonBindingAction
            {
                Id = a.Id,
                CategoryName = a.CategoryName,
                CategoryLabel = a.CategoryLabel,
                Name = a.Name,
                Label = a.Label,
                DefaultKeyboard = a.DefaultKeyboard,
                DefaultKeyboardSource = a.DefaultKeyboardSource,
                DefaultJoystick = a.DefaultJoystick,
                Keyboard = a.Keyboard,
                Joystick = a.Joystick,
                JoystickDesc = a.JoystickDescription,
            }).ToList(),
        };

        return JsonConvert.SerializeObject(json, Formatting.Indented).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    public static string RenderHtml(BindingDocument document, string template)
    {
        if (!template.Contains(BindingsJsonPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Missing {BindingsJsonPlaceholder} placeholder in the HTML template.");
        }

        var embedded = new EmbeddedDocument
        {
            GeneratedAt = document.GeneratedAt,
            Actions = document.Actions.Select(a => new EmbeddedAction
            {
                Id = a.Id,
                CategoryLabel = a.CategoryLabel,
                Label = a.Label,
                Keyboard = a.Keyboard,
                Joystick = a.Joystick,
                JoystickDesc = a.JoystickDescription,
            }).ToList(),
        };

        // '<' becomes \u003c so a localization label cannot close the script tag or start a comment.
        var json = JsonConvert.SerializeObject(embedded, Formatting.None).Replace("<", "\\u003c", StringComparison.Ordinal);
        return template.Replace("\r\n", "\n", StringComparison.Ordinal).Replace(BindingsJsonPlaceholder, json, StringComparison.Ordinal);
    }

    public static int CountRows(BindingDocument document, bool skipMissingCategoryOrDescription) =>
        BuildRows(document, skipMissingCategoryOrDescription).Count;

    private static List<string[]> BuildRows(BindingDocument document, bool skipMissingCategoryOrDescription)
    {
        var rows = new List<string[]>();
        foreach (var action in document.Actions.OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            if (skipMissingCategoryOrDescription
                && (string.IsNullOrWhiteSpace(action.CategoryLabel) || string.IsNullOrWhiteSpace(action.Label)))
            {
                continue;
            }

            rows.Add([action.Id, action.CategoryLabel, action.Label, action.Keyboard, action.Joystick, action.JoystickDescription]);
        }

        return rows;
    }

    // Escape '\' first so a trailing backslash cannot consume the '|' cell separator.
    private static string MarkdownEscape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal);

    private static string TsvEscape(string value) =>
        value.Replace("\t", " ").Replace("\n", " ").Replace("\r", " ");

    // Property order is the field order of the original bindings.json.
    private sealed class JsonBindingDocument
    {
        [JsonProperty("generatedAt")] public string GeneratedAt { get; set; } = string.Empty;
        [JsonProperty("actions")] public List<JsonBindingAction> Actions { get; set; } = new();
    }

    private sealed class JsonBindingAction
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;
        [JsonProperty("categoryName")] public string CategoryName { get; set; } = string.Empty;
        [JsonProperty("categoryLabel")] public string CategoryLabel { get; set; } = string.Empty;
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("label")] public string Label { get; set; } = string.Empty;
        [JsonProperty("defaultKeyboard")] public string DefaultKeyboard { get; set; } = string.Empty;
        [JsonProperty("defaultKeyboardSource")] public string DefaultKeyboardSource { get; set; } = string.Empty;
        [JsonProperty("defaultJoystick")] public string DefaultJoystick { get; set; } = string.Empty;
        [JsonProperty("keyboard")] public string Keyboard { get; set; } = string.Empty;
        [JsonProperty("joystick")] public string Joystick { get; set; } = string.Empty;
        [JsonProperty("joystickDesc")] public string JoystickDesc { get; set; } = string.Empty;
    }

    private sealed class EmbeddedDocument
    {
        [JsonProperty("generatedAt")] public string GeneratedAt { get; set; } = string.Empty;
        [JsonProperty("actions")] public List<EmbeddedAction> Actions { get; set; } = new();
    }

    private sealed class EmbeddedAction
    {
        [JsonProperty("id")] public string Id { get; set; } = string.Empty;
        [JsonProperty("categoryLabel")] public string CategoryLabel { get; set; } = string.Empty;
        [JsonProperty("label")] public string Label { get; set; } = string.Empty;
        [JsonProperty("keyboard")] public string Keyboard { get; set; } = string.Empty;
        [JsonProperty("joystick")] public string Joystick { get; set; } = string.Empty;
        [JsonProperty("joystickDesc")] public string JoystickDesc { get; set; } = string.Empty;
    }
}
