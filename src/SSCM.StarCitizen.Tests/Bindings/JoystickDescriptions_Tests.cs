using System.Xml.Linq;
using NUnit.Framework;
using SSCM.StarCitizen.Bindings;

namespace SSCM.StarCitizen.Tests.Bindings;

public class JoystickDescriptions_Tests
{
    private const string LeftProduct = " Left Stick {1111}";
    private const string RightProduct = " Right Stick {2222}";

    private const string Markdown = """
        # Joystick binding names

        ```yaml
        LT-EVO:
          elite_dangerous_device_id: "AAAA"
          star_citizen_device_id: " Left Stick {1111}"
          bindings:
            "LT-EVO.01(FIRE)":
              elite_dangerous: Joy_1
              star_citizen: button1
            "LT-EVO.POV(HAT_UP)":
              star_citizen: hat1_up
        RT-EVO:
          star_citizen_device_id: " Right Stick {2222}"
          bindings:
            "RT-EVO.01(FIRE)":
              star_citizen: button1
        ```
        """;

    [Test]
    public void LoadJoystickInstances_MapsProductToInstanceId()
    {
        // Arrange
        var actionMaps = ActionMaps($"""
            <options type="keyboard" instance="1" Product="Keyboard" />
            <options type="joystick" instance="1" Product="{LeftProduct}" />
            <options type="joystick" instance="2" Product="{RightProduct}" />
            <options type="joystick" instance="3" />
            """);

        // Act
        var instances = JoystickDescriptions.LoadJoystickInstances(actionMaps);

        // Assert
        Assert.AreEqual(2, instances.Count);
        Assert.AreEqual("js1", instances[LeftProduct]);
        Assert.AreEqual("js2", instances[RightProduct]);
    }

    [Test]
    public void LoadJoystickInstances_ThrowsWhenAProductHasTwoInstances()
    {
        // Arrange
        var actionMaps = ActionMaps($"""
            <options type="joystick" instance="1" Product="{LeftProduct}" />
            <options type="joystick" instance="2" Product="{LeftProduct}" />
            """);

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => JoystickDescriptions.LoadJoystickInstances(actionMaps));
    }

    [Test]
    public void LoadJoystickInstances_ReturnsEmptyWhenNoJoysticksExist()
    {
        // Act
        var instances = JoystickDescriptions.LoadJoystickInstances(ActionMaps(""));

        // Assert
        Assert.IsEmpty(instances);
    }

    [Test]
    public void LoadNames_PrefixesInputsWithTheMatchingJoystickInstance()
    {
        // Arrange
        var instances = new Dictionary<string, string> { [LeftProduct] = "js1", [RightProduct] = "js2" };

        // Act
        var names = JoystickDescriptions.LoadNames(Markdown, instances);

        // Assert
        Assert.AreEqual(3, names.Count);
        Assert.AreEqual("LT-EVO.01(FIRE)", names["js1_button1"]);
        Assert.AreEqual("LT-EVO.POV(HAT_UP)", names["js1_hat1_up"]);
        Assert.AreEqual("RT-EVO.01(FIRE)", names["js2_button1"]);
    }

    [Test]
    public void LoadNames_AcceptsWindowsLineEndings()
    {
        // Arrange
        var instances = new Dictionary<string, string> { [LeftProduct] = "js1", [RightProduct] = "js2" };

        // Act
        var names = JoystickDescriptions.LoadNames(Markdown.Replace("\n", "\r\n"), instances);

        // Assert
        Assert.AreEqual(3, names.Count);
    }

    [Test]
    public void LoadNames_SkipsDevicesWithoutAnInstanceAndWarns()
    {
        // Arrange
        var instances = new Dictionary<string, string> { [LeftProduct] = "js1" };
        var warnings = new List<string>();

        // Act
        var names = JoystickDescriptions.LoadNames(Markdown, instances, warnings.Add);

        // Assert
        Assert.AreEqual(new[] { "js1_button1", "js1_hat1_up" }, names.Keys.OrderBy(k => k));
        Assert.AreEqual(1, warnings.Count);
        Assert.That(warnings[0], Does.Contain(RightProduct).And.Contain("skipped"));
    }

    [Test]
    public void LoadNames_ReturnsNothingWhenNoDeviceIsAttached()
    {
        // Arrange
        var warnings = new List<string>();

        // Act
        var names = JoystickDescriptions.LoadNames(Markdown, new Dictionary<string, string>(), warnings.Add);

        // Assert
        Assert.IsEmpty(names);
        Assert.AreEqual(2, warnings.Count, "one warning per skipped device");
    }

    [Test]
    public void LoadNames_DoesNotRequireAWarningHandler()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => JoystickDescriptions.LoadNames(Markdown, new Dictionary<string, string>()));
    }

    [Test]
    public void LoadNames_ThrowsWhenThereIsNoYamlBlock()
    {
        // Act & Assert
        Assert.Throws<InvalidDataException>(() => JoystickDescriptions.LoadNames("# nothing here", new Dictionary<string, string>()));
    }

    [Test]
    public void LoadNames_ThrowsWhenTwoNamesShareAnInput()
    {
        // Arrange
        var markdown = """
            ```yaml
            LT-EVO:
              star_citizen_device_id: " Left Stick {1111}"
              bindings:
                "LT-EVO.01(A)":
                  star_citizen: button1
                "LT-EVO.02(B)":
                  star_citizen: button1
            ```
            """;

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => JoystickDescriptions.LoadNames(markdown, new Dictionary<string, string> { [LeftProduct] = "js1" }));
    }

    [Test]
    public void Apply_DescribesRebindsIgnoringTheMultiTapSuffix()
    {
        // Arrange
        var names = new Dictionary<string, string> { ["js1_button1"] = "LT-EVO.01(FIRE)" };
        var actions = new List<BindingAction>
        {
            new() { Id = "a", Joystick = "js1_button1" },
            new() { Id = "b", Joystick = "js1_button1 2x" },
        };

        // Act
        JoystickDescriptions.Apply(actions, names);

        // Assert
        Assert.AreEqual(new[] { "LT-EVO.01(FIRE)", "LT-EVO.01(FIRE)" }, actions.Select(a => a.JoystickDescription));
    }

    [Test]
    public void Apply_MarksRebindsWithoutANameAsNotFound()
    {
        // Arrange
        var names = new Dictionary<string, string> { ["js1_button1"] = "LT-EVO.01(FIRE)" };
        var actions = new List<BindingAction>
        {
            new() { Id = "a", Joystick = "js1_button99" },
            new() { Id = "b", Joystick = "js3_button1 3x" },
        };

        // Act
        JoystickDescriptions.Apply(actions, names);

        // Assert
        Assert.AreEqual(new[] { "{NOT_FOUND}", "{NOT_FOUND}" }, actions.Select(a => a.JoystickDescription));
    }

    [Test]
    public void Apply_LeavesNothingToDescribeEmpty()
    {
        // Arrange
        var names = new Dictionary<string, string> { ["js1_button1"] = "LT-EVO.01(FIRE)" };
        var actions = new List<BindingAction>
        {
            new() { Id = "none", Joystick = "", JoystickDescription = "stale" },
            new() { Id = "unbound", Joystick = "{UNBOUND}" },
            new() { Id = "defaultUnbound", Joystick = "(UNBOUND)" },
            new() { Id = "defaultAxis", Joystick = "(y)" },
        };

        // Act
        JoystickDescriptions.Apply(actions, names);

        // Assert
        Assert.AreEqual(new[] { "", "", "", "" }, actions.Select(a => a.JoystickDescription));
    }

    [Test]
    public void Apply_WithoutAnyNamesMarksEveryRebindNotFound()
    {
        // Arrange
        var actions = new List<BindingAction>
        {
            new() { Id = "a", Joystick = "js1_button1" },
            new() { Id = "b", Joystick = "{UNBOUND}" },
        };

        // Act
        JoystickDescriptions.Apply(actions, new Dictionary<string, string>());

        // Assert
        Assert.AreEqual(new[] { "{NOT_FOUND}", "" }, actions.Select(a => a.JoystickDescription));
    }

    private static XDocument ActionMaps(string options) =>
        XDocument.Parse($"<ActionMaps><ActionProfiles profileName=\"default\">{options}</ActionProfiles></ActionMaps>");
}
