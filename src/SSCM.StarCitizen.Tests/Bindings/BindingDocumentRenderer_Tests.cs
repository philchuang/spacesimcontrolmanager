using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SSCM.Core;

namespace SSCM.StarCitizen.Tests.Bindings;

public class BindingDocumentRenderer_Tests
{
    [Test]
    public void RenderMarkdown_WritesTableSortedByIdAndSkipsRowsWithoutCategoryOrDescription()
    {
        // Arrange
        var document = Document(
            Action("b-two", "Cat", "Two", "(x)", "js1_button1", "LT-EVO.01(FIRE)"),
            Action("a-one", "Cat", "One", "(y)", "", ""),
            Action("c-nocategory", "", "No category", "", "", ""),
            Action("d-nodescription", "Cat", " ", "", "", ""));

        // Act
        var markdown = BindingDocumentRenderer.RenderMarkdown(document, "Star Citizen Bindings");

        // Assert
        var expected = string.Join("\n",
            "# Star Citizen Bindings",
            "",
            "## GENERATED BINDINGS",
            "",
            "|id|category|description|keyboard|joystick|joystickDesc|",
            "|---|---|---|---|---|---|",
            "|a-one|Cat|One|(y)|||",
            "|b-two|Cat|Two|(x)|js1_button1|LT-EVO.01(FIRE)|") + "\n";
        Assert.AreEqual(expected, markdown);
    }

    [Test]
    public void RenderMarkdown_EscapesPipesAndLineBreaks()
    {
        // Arrange
        var document = Document(Action("a", "Cat", "A|B\r\nC", "", "", ""));

        // Act
        var markdown = BindingDocumentRenderer.RenderMarkdown(document, "Star Citizen Bindings");

        // Assert
        Assert.That(markdown, Does.Contain("|a|Cat|A\\|B  C|"));
    }

    [Test]
    public void RenderTsv_KeepsEveryRowAndFlattensTabsAndLineBreaks()
    {
        // Arrange
        var document = Document(
            Action("b", "Cat", "Two\tTabbed\nLine", "", "", ""),
            Action("a", "", "", "(y)", "", ""));

        // Act
        var tsv = BindingDocumentRenderer.RenderTsv(document);

        // Assert
        var expected = string.Join("\n",
            "id\tcategory\tdescription\tkeyboard\tjoystick\tjoystickDesc",
            "a\t\t\t(y)\t\t",
            "b\tCat\tTwo Tabbed Line\t\t\t") + "\n";
        Assert.AreEqual(expected, tsv);
    }

    [Test]
    public void RenderHtml_EmbedsDocumentJsonAndEscapesScriptClosers()
    {
        // Arrange
        var document = Document(Action("a", "Cat", "</script><b>", "(x)", "js1_button1", "LT-EVO.01(FIRE)"));
        document.GeneratedAt = "2026-10-15T12:00:00-04:00";
        const string template = "<script>const data = __BINDINGS_JSON__;</script>";

        // Act
        var html = BindingDocumentRenderer.RenderHtml(document, template);

        // Assert
        Assert.That(html, Does.Not.Contain(BindingDocumentRenderer.BindingsJsonPlaceholder));
        Assert.That(html, Does.Not.Contain("</script><b>"));
        Assert.That(html, Does.Contain("<\\/script>"));

        var json = html["<script>const data = ".Length..^(";</script>".Length)];
        var parsed = JsonConvert.DeserializeObject<JObject>(json, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None })!;
        Assert.AreEqual("2026-10-15T12:00:00-04:00", (string?) parsed["generatedAt"]);
        var action = (JObject) parsed["actions"]![0]!;
        Assert.AreEqual("</script><b>", (string?) action["label"]);
        Assert.AreEqual("Cat", (string?) action["categoryLabel"]);
        Assert.AreEqual("LT-EVO.01(FIRE)", (string?) action["joystickDesc"]);
        Assert.AreEqual(new[] { "id", "categoryLabel", "label", "keyboard", "joystick", "joystickDesc" }, action.Properties().Select(p => p.Name));
    }

    [Test]
    public void RenderJson_WritesEveryFieldInOrderWithTwoSpaceIndentAndLfOnly()
    {
        // Arrange
        var document = Document(new BindingAction
        {
            Id = "seat-fire",
            CategoryName = "seat",
            CategoryLabel = "Seat Controls",
            Name = "fire",
            Label = "Fire \"Weapons\"",
            DefaultKeyboard = "space",
            DefaultKeyboardSource = "keyboard",
            DefaultJoystick = "js1_button1",
            Keyboard = "kb1_f",
            Joystick = "js1_button1 2x",
            JoystickDescription = "{NOT_FOUND}",
        });
        document.GeneratedAt = "2026-10-15T12:00:00-04:00";

        // Act
        var json = BindingDocumentRenderer.RenderJson(document);

        // Assert
        var expected = string.Join("\n",
            "{",
            "  \"generatedAt\": \"2026-10-15T12:00:00-04:00\",",
            "  \"actions\": [",
            "    {",
            "      \"id\": \"seat-fire\",",
            "      \"categoryName\": \"seat\",",
            "      \"categoryLabel\": \"Seat Controls\",",
            "      \"name\": \"fire\",",
            "      \"label\": \"Fire \\\"Weapons\\\"\",",
            "      \"defaultKeyboard\": \"space\",",
            "      \"defaultKeyboardSource\": \"keyboard\",",
            "      \"defaultJoystick\": \"js1_button1\",",
            "      \"keyboard\": \"kb1_f\",",
            "      \"joystick\": \"js1_button1 2x\",",
            "      \"joystickDesc\": \"{NOT_FOUND}\"",
            "    }",
            "  ]",
            "}") + "\n";
        Assert.AreEqual(expected, json);
    }

    [Test]
    public void RenderJson_KeepsRowsWithoutACategoryUnlikeTheMarkdown()
    {
        // Arrange
        var document = Document(Action("b", "", "", "", "", ""), Action("a", "Cat", "A", "", "", ""));

        // Act
        var json = JObject.Parse(BindingDocumentRenderer.RenderJson(document), new JsonLoadSettings());

        // Assert
        Assert.AreEqual(new[] { "b", "a" }, ((JArray) json["actions"]!).Select(a => (string?) a["id"]));
    }

    [Test]
    public void RenderHtml_ThrowsWhenTemplateHasNoPlaceholder()
    {
        // Act & Assert
        Assert.Throws<InvalidDataException>(() => BindingDocumentRenderer.RenderHtml(Document(), "<html></html>"));
    }

    [Test]
    public void RenderHtml_NormalizesTemplateLineEndings()
    {
        // Act
        var html = BindingDocumentRenderer.RenderHtml(Document(), "<a>\r\n__BINDINGS_JSON__\r\n</a>");

        // Assert
        Assert.That(html, Does.Not.Contain("\r"));
    }

    private static BindingDocument Document(params BindingAction[] actions) =>
        new() { GeneratedAt = "2026-01-01T00:00:00+00:00", Actions = actions.ToList() };

    private static BindingAction Action(string id, string category, string label, string keyboard, string joystick, string joystickDesc) =>
        new() { Id = id, CategoryLabel = category, Label = label, Keyboard = keyboard, Joystick = joystick, JoystickDescription = joystickDesc };
}
