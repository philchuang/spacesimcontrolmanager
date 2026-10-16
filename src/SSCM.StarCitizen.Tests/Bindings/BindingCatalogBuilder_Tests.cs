using System.Xml.Linq;
using NUnit.Framework;
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
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization, ActionMaps(""));

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
            <action name="c" /></actionmap>
            """);

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization, ActionMaps("")).ToDictionary(a => a.Id);

        // Assert
        Assert.AreEqual("(mouse1)", actions["m-a"].Keyboard);
        Assert.AreEqual("(space)", actions["m-b"].Keyboard);
        Assert.AreEqual("", actions["m-c"].Keyboard);
    }

    [Test]
    public void Build_KeepsRawNamesAndDefaultsWithTheirSource()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="m" UILabel="Map"><action name="a" keyboard=" " mouse="mouse1" joystick="js1_button4" />
            <action name="b" keyboard="space" mouse="mouse2" />
            <action name="c" /></actionmap>
            """);

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization, ActionMaps("")).ToDictionary(a => a.Id);

        // Assert
        Assert.AreEqual("m", actions["m-a"].CategoryName);
        Assert.AreEqual("a", actions["m-a"].Name);
        Assert.AreEqual("mouse1", actions["m-a"].DefaultKeyboard);
        Assert.AreEqual("mouse", actions["m-a"].DefaultKeyboardSource);
        Assert.AreEqual("js1_button4", actions["m-a"].DefaultJoystick);
        Assert.AreEqual("space", actions["m-b"].DefaultKeyboard);
        Assert.AreEqual("keyboard", actions["m-b"].DefaultKeyboardSource);
        Assert.AreEqual("", actions["m-c"].DefaultKeyboard);
        Assert.AreEqual("keyboard", actions["m-c"].DefaultKeyboardSource);
    }

    [Test]
    public void Build_WrapsDefaultsAndMarksTrailingSpaceAsUnbound()
    {
        // Arrange
        var profile = Profile("""<actionmap name="m"><action name="a" keyboard="u+lshift" joystick=" " /></actionmap>""");

        // Act
        var action = BindingCatalogBuilder.Build(profile, NoLocalization, ActionMaps("")).Single();

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
        var actions = BindingCatalogBuilder.Build(profile, localization, ActionMaps("")).ToDictionary(a => a.Id);

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
        var actions = BindingCatalogBuilder.Build(profile, null, ActionMaps("")).ToDictionary(a => a.Id);

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
        var action = BindingCatalogBuilder.Build(profile, NoLocalization, ActionMaps("")).Single();

        // Assert
        Assert.AreEqual("Category Text", action.CategoryLabel);
        Assert.AreEqual("Plain Text", action.Label);
    }

    [Test]
    public void Build_AppliesRebindsByDeviceKindAndKeepsTheFirstUniqueOne()
    {
        // Arrange
        var profile = Profile("""<actionmap name="seat"><action name="fire" keyboard="space" joystick="js1_button9" /></actionmap>""");
        var actionMaps = ActionMaps("""
            <actionmap name="seat"><action name="fire">
              <rebind input="kb1_f" />
              <rebind input="js1_button3" />
              <rebind input="kb1_f" />
              <rebind input="kb1_g" />
              <rebind input="mo1_mouse1" />
            </action></actionmap>
            """);

        // Act
        var action = BindingCatalogBuilder.Build(profile, NoLocalization, actionMaps).Single();

        // Assert
        Assert.AreEqual("kb1_f", action.Keyboard);
        Assert.AreEqual("js1_button3", action.Joystick);
    }

    [Test]
    public void Build_MarksTrailingSpaceRebindsUnboundAndAppendsMultiTapSuffix()
    {
        // Arrange
        var profile = Profile("""
            <actionmap name="s"><action name="a" /><action name="b" /><action name="c" /><action name="d" /></actionmap>
            """);
        var actionMaps = ActionMaps("""
            <actionmap name="s">
              <action name="a"><rebind input="js1_ " /></action>
              <action name="b"><rebind input="js1_button1" multiTap="2" /></action>
              <action name="c"><rebind input="js1_ " multiTap="2" /></action>
              <action name="d"><rebind input="kb1_x" multiTap=" 3 " /></action>
            </actionmap>
            """);

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
        var actionMaps = ActionMaps("""<actionmap name="seat"><action name="removed"><rebind input="kb1_x" /></action></actionmap>""");

        // Act
        var actions = BindingCatalogBuilder.Build(profile, NoLocalization, actionMaps);

        // Assert
        Assert.AreEqual(new[] { "seat-fire" }, actions.Select(a => a.Id));
    }

    private static XDocument Profile(string actionMaps) => XDocument.Parse($"<profile>{actionMaps}</profile>");

    private static XDocument ActionMaps(string actionMaps) =>
        XDocument.Parse($"<ActionMaps><ActionProfiles profileName=\"default\">{actionMaps}</ActionProfiles></ActionMaps>");
}
