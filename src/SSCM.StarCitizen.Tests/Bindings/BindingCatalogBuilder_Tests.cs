using System.Xml.Linq;
using NUnit.Framework;
using SSCM.Core;
using SSCM.StarCitizen;
using SSCM.StarCitizen.Bindings;

namespace SSCM.StarCitizen.Tests.Bindings;

public class BindingCatalogBuilder_Tests
{
    private static readonly Dictionary<string, string> NoLocalization = new();

    [Test]
    public void Build_CreatesIdFromCategoryAndActionNameSortedOrdinally()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="seat" UILabel="Seat"><action name="fire" UILabel="Fire" /></actionmap>
            <actionmap name="Flight" UILabel="Flight"><action name="boost" UILabel="Boost" /></actionmap>
            """);

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization);

        // Assert
        Assert.AreEqual(new[] { "Flight-boost", "seat-fire" }, actions.Select(a => a.Id));
        Assert.AreEqual("Flight", actions[0].CategoryLabel);
        Assert.AreEqual("Boost", actions[0].Label);
    }

    [Test]
    public void Build_UsesMouseDefaultWhenKeyboardIsBlank()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="m"><action name="a" keyboard=" " mouse="mouse1" />
            <action name="b" keyboard="space" mouse="mouse2" />
            <action name="c" />
            <action name="d" keyboard="space" mouse=" " /></actionmap>
            """);

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization).ToDictionary(a => a.Id);

        // Assert
        Assert.AreEqual("(mouse1)", actions["m-a"].Keyboard);
        Assert.AreEqual("(mouse2)", actions["m-b"].Keyboard, "when both are set, mouse wins");
        Assert.AreEqual("", actions["m-c"].Keyboard);
        Assert.AreEqual("(space)", actions["m-d"].Keyboard);
    }

    [Test]
    public void Build_KeepsRawNamesAndDefaultsWithTheirSource()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="m" UILabel="Map"><action name="a" keyboard=" " mouse="mouse1" joystick="js1_button4" />
            <action name="b" keyboard="space" mouse="mouse2" />
            <action name="c" />
            <action name="d" keyboard="space" /></actionmap>
            """);

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization).ToDictionary(a => a.Id);

        // Assert
        Assert.AreEqual("m", actions["m-a"].CategoryName);
        Assert.AreEqual("a", actions["m-a"].Name);
        Assert.AreEqual("mouse1", actions["m-a"].DefaultKeyboard);
        Assert.AreEqual("mouse", actions["m-a"].DefaultKeyboardSource);
        Assert.AreEqual("js1_button4", actions["m-a"].DefaultJoystick);
        Assert.AreEqual("mouse2", actions["m-b"].DefaultKeyboard);
        Assert.AreEqual("mouse", actions["m-b"].DefaultKeyboardSource);
        Assert.AreEqual("", actions["m-c"].DefaultKeyboard);
        Assert.AreEqual("keyboard", actions["m-c"].DefaultKeyboardSource);
        Assert.AreEqual("space", actions["m-d"].DefaultKeyboard);
        Assert.AreEqual("keyboard", actions["m-d"].DefaultKeyboardSource);
    }

    [Test]
    public void Build_WrapsDefaultsAndMarksTrailingSpaceAsUnbound()
    {
        // Arrange
        var profile = Profile("""<actionmap name="m"><action name="a" keyboard="u+lshift" joystick=" " /></actionmap>""");

        // Act
        var action = BindingCatalogBuilder.Build(profile, NoLocalization).Single();

        // Assert
        Assert.AreEqual("(u+lshift)", action.Keyboard);
        Assert.AreEqual("(UNBOUND)", action.Joystick);
    }

    [Test]
    public void Build_LocalizesLabelsByExactKeyThenByKeyWithoutAtSign()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="m" UILabel="@ui_cat">
              <action name="a" UILabel="@ui_a" />
              <action name="b" UILabel="ui_b" />
              <action name="c" UILabel="@unknown" />
              <action name="d" UILabel="@ui_d" />
            </actionmap>
            """);
        var localization = new Dictionary<string, string>
        {
            ["ui_cat"] = "Category",
            ["ui_a"] = "Alpha",
            ["ui_b"] = "Bravo",
            ["@ui_d"] = "Exact",
            ["ui_d"] = "WithoutAt",
        };

        // Act
        var actions = BindingCatalogBuilder.Build(profile, localization).ToDictionary(a => a.Id);

        // Assert
        Assert.AreEqual("Category", actions["m-a"].CategoryLabel, "category labels are localized too");
        Assert.AreEqual("Alpha", actions["m-a"].Label, "falls back to the key without the leading '@'");
        Assert.AreEqual("Bravo", actions["m-b"].Label, "plain keys match exactly");
        Assert.AreEqual("@unknown/KNF", actions["m-c"].Label, "unknown keys get a KNF suffix");
        Assert.AreEqual("Exact", actions["m-d"].Label, "an exact match wins over the '@'-stripped key");
    }

    [Test]
    public void Build_MarksLocalizationKeysAsFileNotFoundWhenThereIsNoLocalizationFile()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="m" UILabel="@ui_cat">
              <action name="a" UILabel="@ui_a" />
              <action name="b" UILabel="Plain Text" />
              <action name="c" />
            </actionmap>
            """);

        // Act
        var actions = BindingCatalogBuilder.Build(profile, null).ToDictionary(a => a.Id);

        // Assert
        Assert.AreEqual("@ui_cat/FNF", actions["m-a"].CategoryLabel);
        Assert.AreEqual("@ui_a/FNF", actions["m-a"].Label);
        Assert.AreEqual("Plain Text", actions["m-b"].Label, "literal text is not a localization key");
        Assert.AreEqual(string.Empty, actions["m-c"].Label, "a blank label stays blank");
    }

    [Test]
    public void Build_LeavesLiteralTextAloneWhenItsKeyIsMissingFromTheLocalizationFile()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="m" UILabel="Category Text">
              <action name="a" UILabel="Plain Text" />
            </actionmap>
            """);

        // Act
        var action = BindingCatalogBuilder.Build(profile, NoLocalization).Single();

        // Assert
        Assert.AreEqual("Category Text", action.CategoryLabel);
        Assert.AreEqual("Plain Text", action.Label);
    }

    [Test]
    public void Build_AppliesRebindsByDeviceKindAndKeepsTheFirstUniqueOne()
    {
        // Arrange
        var profile = Profile("""<actionmap name="seat"><action name="fire" keyboard="space" joystick="js1_button9" /></actionmap>""");
        var actionMaps = Data(
            Map("seat", "fire", "kb1_f", "keyboard"),
            Map("seat", "fire", "js1_button3", "joystick"),
            Map("seat", "fire", "kb1_f", "keyboard"),
            Map("seat", "fire", "kb1_g", "keyboard"),
            Map("seat", "fire", "mo1_mouse1", "mouse"));

        // Act
        var action = BindingCatalogBuilder.Build(profile, NoLocalization, actionMaps).Single();

        // Assert
        Assert.AreEqual("kb1_f", action.Keyboard);
        Assert.AreEqual("js1_button3", action.Joystick);
    }

    [Test]
    public void Build_PutsMouseRebindsInTheKeyboardColumn()
    {
        // Arrange
        var profile = Profile("""<actionmap name="seat"><action name="look" mouse="maxis_x" /></actionmap>""");
        var actionMaps = Data(Map("seat", "look", "mo1_mouse1", "mouse"));

        // Act
        var action = BindingCatalogBuilder.Build(profile, NoLocalization, actionMaps).Single();

        // Assert
        Assert.AreEqual("mo1_mouse1", action.Keyboard);
        Assert.AreEqual("", action.Joystick);
    }

    [Test]
    public void Build_IgnoresGamepadRebinds()
    {
        // Arrange
        var profile = Profile("""<actionmap name="seat"><action name="fire" keyboard="space" /></actionmap>""");
        var actionMaps = Data(Map("seat", "fire", "gp1_button1", "gamepad"));

        // Act
        var action = BindingCatalogBuilder.Build(profile, NoLocalization, actionMaps).Single();

        // Assert
        Assert.AreEqual("(space)", action.Keyboard);
        Assert.AreEqual("", action.Joystick);
    }

    [Test]
    public void Build_MarksTrailingSpaceRebindsUnboundAndAppendsMultiTapSuffix()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="s"><action name="a" /><action name="b" /><action name="c" /><action name="d" /></actionmap>
            """);
        var actionMaps = Data(
            Map("s", "a", "js1_ ", "joystick"),
            Map("s", "b", "js1_button1", "joystick", multiTap: 2),
            Map("s", "c", "js1_ ", "joystick", multiTap: 2),
            Map("s", "d", "kb1_x", "keyboard", multiTap: 3));

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization, actionMaps).ToDictionary(a => a.Id);

        // Assert
        Assert.AreEqual("{UNBOUND}", actions["s-a"].Joystick);
        Assert.AreEqual("js1_button1 2x", actions["s-b"].Joystick);
        Assert.AreEqual("{UNBOUND}", actions["s-c"].Joystick, "unbound inputs never get a multi-tap suffix");
        Assert.AreEqual("kb1_x 3x", actions["s-d"].Keyboard);
    }

    [Test]
    public void Build_IgnoresRebindsForActionsNotInTheDefaultProfile()
    {
        // Arrange
        var profile = Profile("""<actionmap name="seat"><action name="fire" /></actionmap>""");
        var actionMaps = Data(Map("seat", "removed", "kb1_x", "keyboard"));

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization, actionMaps);

        // Assert
        Assert.AreEqual(new[] { "seat-fire" }, actions.Select(a => a.Id));
    }

    private static XDocument Profile(string actionMaps) => XDocument.Parse($"<profile>{actionMaps}</profile>");

    private static SCMappingData Data(params SCMapping[] mappings) => new() { Mappings = mappings };

    private static SCMapping Map(string actionMap, string action, string input, string inputType, int? multiTap = null) =>
        new() { ActionMap = actionMap, Action = action, Input = input, InputType = inputType, MultiTap = multiTap };
}
