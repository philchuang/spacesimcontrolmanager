using NUnit.Framework;
using SSCM.Core;
using SSCM.StarCitizen;
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
        var inputs = new[]
        {
            Device("keyboard", 1, "Keyboard"),
            Device("joystick", 1, LeftProduct),
            Device("joystick", 2, RightProduct),
            Device("joystick", 3, ""),
        };

        // Act
        var instances = SCJoystickDescriptions.LoadJoystickInstances(inputs);

        // Assert
        Assert.AreEqual(2, instances.Count);
        Assert.AreEqual("js1", instances[LeftProduct]);
        Assert.AreEqual("js2", instances[RightProduct]);
    }

    [Test]
    public void LoadJoystickInstances_ThrowsWhenAProductHasTwoInstances()
    {
        // Arrange
        var inputs = new[]
        {
            Device("joystick", 1, LeftProduct),
            Device("joystick", 2, LeftProduct),
        };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => SCJoystickDescriptions.LoadJoystickInstances(inputs));
    }

    [Test]
    public void LoadJoystickInstances_ReturnsEmptyWhenNoJoysticksExist()
    {
        // Act
        var instances = SCJoystickDescriptions.LoadJoystickInstances([Device("keyboard", 1, "Keyboard")]);

        // Assert
        Assert.IsEmpty(instances);
    }

    [Test]
    public void WarnUnattached_WarnsForYamlDevicesMissingFromActionMaps()
    {
        // Arrange
        var descriptions = JoystickBindingsFile.Parse(Markdown);
        var instances = new Dictionary<string, string> { [LeftProduct] = "js1" };
        var warnings = new List<string>();

        // Act
        SCJoystickDescriptions.WarnUnattached(descriptions, instances, warnings.Add);

        // Assert
        Assert.AreEqual("LT-EVO.01(FIRE)", descriptions.Get(SCJoystickDescriptions.Game, LeftProduct)["button1"]);
        Assert.AreEqual(1, warnings.Count);
        Assert.That(warnings[0], Does.Contain(RightProduct).And.Contain("skipped"));
    }

    [Test]
    public void WarnUnattached_DoesNotRequireAWarningHandler()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => SCJoystickDescriptions.WarnUnattached(JoystickBindingsFile.Parse(Markdown), new Dictionary<string, string>(), null));
    }

    [Test]
    public void Apply_DescribesRebindsIgnoringTheMultiTapSuffix()
    {
        // Arrange
        var descriptions = JoystickBindingsFile.Parse(Markdown);
        var instances = new Dictionary<string, string> { [LeftProduct] = "js1", [RightProduct] = "js2" };
        var actions = new List<BindingAction>
        {
            new() { Id = "a", Joystick = "js1_button1" },
            new() { Id = "b", Joystick = "js1_button1 2x" },
            new() { Id = "c", Joystick = "js1_hat1_up" },
            new() { Id = "d", Joystick = "js2_button1" },
        };

        // Act
        SCJoystickDescriptions.Apply(actions, descriptions, instances);

        // Assert
        Assert.AreEqual(
            new[] { "LT-EVO.01(FIRE)", "LT-EVO.01(FIRE)", "LT-EVO.POV(HAT_UP)", "RT-EVO.01(FIRE)" },
            actions.Select(a => a.JoystickDescription));
    }

    [Test]
    public void Apply_MarksRebindsWithoutANameAsNotFound()
    {
        // Arrange
        var descriptions = JoystickBindingsFile.Parse(Markdown);
        var instances = new Dictionary<string, string> { [LeftProduct] = "js1" };
        var actions = new List<BindingAction>
        {
            new() { Id = "a", Joystick = "js1_button99" },
            new() { Id = "b", Joystick = "js3_button1 3x" },
        };

        // Act
        SCJoystickDescriptions.Apply(actions, descriptions, instances);

        // Assert
        Assert.AreEqual(new[] { "{NOT_FOUND}", "{NOT_FOUND}" }, actions.Select(a => a.JoystickDescription));
    }

    [Test]
    public void Apply_LeavesNothingToDescribeEmpty()
    {
        // Arrange
        var actions = new List<BindingAction>
        {
            new() { Id = "none", Joystick = "", JoystickDescription = "stale" },
            new() { Id = "unbound", Joystick = "{UNBOUND}" },
            new() { Id = "defaultUnbound", Joystick = "(UNBOUND)" },
            new() { Id = "defaultAxis", Joystick = "(y)" },
        };

        // Act
        SCJoystickDescriptions.Apply(actions, JoystickBindingsFile.Parse(Markdown), new Dictionary<string, string> { [LeftProduct] = "js1" });

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
        SCJoystickDescriptions.Apply(actions, JoystickDescriptions.Empty, new Dictionary<string, string> { [LeftProduct] = "js1" });

        // Assert
        Assert.AreEqual(new[] { "{NOT_FOUND}", "" }, actions.Select(a => a.JoystickDescription));
    }

    private static SCInputDevice Device(string type, int instance, string product) =>
        new() { Type = type, Instance = instance, Product = product };
}
