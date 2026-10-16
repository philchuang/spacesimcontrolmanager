using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using SSCM.Elite.Bindings;

namespace SSCM.Elite.Tests.Bindings;

public class EDJoystickDescriptions_Tests
{
    private const string Markdown = """
        # Joystick binding names

        ```yaml
        LT-EVO:
          elite_dangerous_device_id: "231D0200"
          star_citizen_device_id: " Left Stick {1111}"
          bindings:
            "LT-EVO.01(FIRE)":
              elite_dangerous: Joy_1
              star_citizen: button1
            "LT-EVO.04(B1)":
              elite_dangerous: Joy_4
            "LT-EVO.81(HAT)":
              elite_dangerous: Joy_81
        RT-EVO:
          elite_dangerous_device_id: "231D3205"
          bindings:
            "RT-EVO.RZ(TWIST)":
              elite_dangerous: Joy_RZAxis
        UNUSED:
          elite_dangerous_device_id: "DEADBEEF"
          bindings:
            "UNUSED.01":
              elite_dangerous: Joy_1
        ```
        """;

    [Test]
    public void LoadAttachedDeviceIds_IgnoresKeyboardMouseAndUnbound()
    {
        // Arrange
        var data = new EDMappingData
        {
            Mappings =
            [
                new("Ship", "A") { Primary = new EDBinding("Keyboard", "Key_A"), Secondary = new EDBinding("231D0200", "Joy_4") },
                new("Ship", "B") { Binding = EDBinding.UNBOUND() },
                new("Ship", "C") { Primary = new EDBinding("Mouse", "Mouse_1") },
                new("Ship", "D") { Primary = new EDBinding("231D0200", "Joy_1", [new EDBindingKey("231D3205", "Joy_81")]) },
            ],
        };

        // Act
        var ids = EDJoystickDescriptions.LoadAttachedDeviceIds(data);

        // Assert
        Assert.AreEqual(new[] { "231D0200", "231D3205" }, ids.OrderBy(id => id));
    }

    [Test]
    public void WarnUnattached_WarnsForYamlDevicesMissingFromTheBindsFile()
    {
        // Arrange
        var descriptions = JoystickBindingsFile.Parse(Markdown);
        var attached = new HashSet<string> { "231D0200" };
        var warnings = new List<string>();

        // Act
        EDJoystickDescriptions.WarnUnattached(descriptions, attached, warnings.Add);

        // Assert
        Assert.AreEqual("LT-EVO.01(FIRE)", descriptions.Get(EDJoystickDescriptions.Game, "231D0200")["Joy_1"]);
        Assert.AreEqual("RT-EVO.RZ(TWIST)", descriptions.Get(EDJoystickDescriptions.Game, "231D3205")["Joy_RZAxis"]);
        Assert.AreEqual(2, warnings.Count);
        Assert.That(warnings[0], Does.Contain("231D3205").And.Contain("skipped"));
        Assert.That(warnings[1], Does.Contain("DEADBEEF"));
    }

    [Test]
    public void Apply_DescribesEachJoystickSlotAndMarksUnknownInputs()
    {
        // Arrange
        var descriptions = JoystickBindingsFile.Parse(Markdown);
        var actions = new List<BindingAction>
        {
            new() { Id = "a", Joystick = "231D0200-Joy_1" },
            new() { Id = "b", Joystick = "231D0200-Joy_1; 231D3205-Joy_RZAxis" },
            new() { Id = "c", Joystick = "231D0200-Joy_99" },
            new() { Id = "d", Joystick = "231D0200-Joy_1 + 231D0200-Joy_81" },
            new() { Id = "e", Joystick = "(231D0200-Joy_1)" },
            new() { Id = "f", Joystick = "" },
            new() { Id = "g", Joystick = "231D0200-Joy_1 + Keyboard-Key_LeftControl" },
            new() { Id = "h", Joystick = "231D0200-Joy_1 + 231D0200-Joy_99" },
            new() { Id = "i", Joystick = "231D3205-Neg_Joy_RZAxis" },
        };

        // Act
        EDJoystickDescriptions.Apply(actions, descriptions);

        // Assert
        Assert.AreEqual(
            new[]
            {
                "LT-EVO.01(FIRE)",
                "LT-EVO.01(FIRE); RT-EVO.RZ(TWIST)",
                "{NOT_FOUND}",
                "LT-EVO.01(FIRE) + LT-EVO.81(HAT)",
                "",
                "",
                "LT-EVO.01(FIRE) + Key_LeftControl",
                "LT-EVO.01(FIRE) + {NOT_FOUND}",
                "RT-EVO.RZ(TWIST)",
            },
            actions.Select(a => a.JoystickDescription));
    }
}
