using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using SSCM.Elite.Bindings;

namespace SSCM.Elite.Tests.Bindings;

public class EDBindingCatalogBuilder_Tests
{
    [Test]
    public void Build_WithoutDefaults_FoldsKeyboardAndJoystickFromCustomBinds()
    {
        // Arrange
        var custom = Data(
            Mapping("Ship-FlightRotation", "YawLeftButton",
                primary: Bind("Keyboard", "Key_A"),
                secondary: Bind("231D0200", "Joy_4")));

        // Act
        var actions = EDBindingCatalogBuilder.Build(custom);

        // Assert
        var action = AssertSingle(actions);
        Assert.AreEqual("Ship-FlightRotation.YawLeftButton", action.Id);
        Assert.AreEqual("Ship-FlightRotation", action.CategoryName);
        Assert.AreEqual("Ship-FlightRotation", action.CategoryLabel);
        Assert.AreEqual("YawLeftButton", action.Name);
        Assert.AreEqual("YawLeftButton", action.Label);
        Assert.AreEqual("Keyboard-Key_A", action.Keyboard);
        Assert.AreEqual("231D0200-Joy_4", action.Joystick);
        Assert.AreEqual("", action.DefaultKeyboard);
        Assert.AreEqual("", action.DefaultJoystick);
    }

    [Test]
    public void Build_PutsMouseInTheKeyboardColumn()
    {
        // Arrange
        var custom = Data(Mapping("General-InterfaceMode", "UI_Select", primary: Bind("Mouse", "Mouse_1")));

        // Act
        var actions = EDBindingCatalogBuilder.Build(custom);

        // Assert
        Assert.AreEqual("Mouse-Mouse_1", AssertSingle(actions).Keyboard);
        Assert.AreEqual("", AssertSingle(actions).Joystick);
    }

    [Test]
    public void Build_JoinsDifferentJoystickDevicesAndKeepsModifiers()
    {
        // Arrange
        var custom = Data(Mapping("Ship-Weapons", "PrimaryFire",
            primary: Bind("231D0200", "Joy_1"),
            secondary: Bind("231D3205", "Joy_4", ("231D3205", "Joy_81"))));

        // Act
        var action = AssertSingle(EDBindingCatalogBuilder.Build(custom));

        // Assert
        Assert.AreEqual("", action.Keyboard);
        Assert.AreEqual("231D0200-Joy_1; 231D3205-Joy_4 + 231D3205-Joy_81", action.Joystick);
    }

    [Test]
    public void Build_SameDeviceSecondaryLosesToPrimary()
    {
        // Arrange
        var custom = Data(Mapping("Ship-Weapons", "PrimaryFire",
            primary: Bind("231D0200", "Joy_1"),
            secondary: Bind("231D0200", "Joy_4")));

        // Act
        var action = AssertSingle(EDBindingCatalogBuilder.Build(custom));

        // Assert
        Assert.AreEqual("231D0200-Joy_1", action.Joystick);
    }

    [Test]
    public void Build_UsesAxisBindingSlot()
    {
        // Arrange
        var custom = Data(Mapping("Ship-FlightRotation", "YawAxisRaw", binding: Bind("231D3205", "Joy_RZAxis")));

        // Act
        var action = AssertSingle(EDBindingCatalogBuilder.Build(custom));

        // Assert
        Assert.AreEqual("231D3205-Joy_RZAxis", action.Joystick);
        Assert.AreEqual("", action.Keyboard);
    }

    [Test]
    public void Build_SkipsUnboundSlots()
    {
        // Arrange
        var custom = Data(Mapping("Ship-FlightRotation", "YawLeftButton",
            primary: EDBinding.UNBOUND(),
            secondary: Bind("Keyboard", "Key_A")));

        // Act
        var action = AssertSingle(EDBindingCatalogBuilder.Build(custom));

        // Assert
        Assert.AreEqual("Keyboard-Key_A", action.Keyboard);
        Assert.AreEqual("", action.Joystick);
    }

    [Test]
    public void Build_WithoutLocalization_UsesTheXmlActionName()
    {
        // Arrange
        var custom = Data(Mapping("Ship-FlightRotation", "YawLeftButton", primary: Bind("Keyboard", "Key_A")));

        // Act
        var action = AssertSingle(EDBindingCatalogBuilder.Build(custom));

        // Assert
        Assert.AreEqual("YawLeftButton", action.Label);
    }

    [Test]
    public void Build_WithLocalization_ReplacesKnownNamesAndMarksMisses()
    {
        // Arrange
        var custom = Data(
            Mapping("Ship-FlightRotation", "YawLeftButton", primary: Bind("Keyboard", "Key_A")),
            Mapping("Ship-FlightRotation", "PitchUpButton", primary: Bind("Keyboard", "Key_B")));
        var localization = new Dictionary<string, string> { ["YawLeftButton"] = "Yaw Left" };

        // Act
        var actions = EDBindingCatalogBuilder.Build(custom, localization: localization);

        // Assert
        Assert.AreEqual("Yaw Left", actions.Single(a => a.Name == "YawLeftButton").Label);
        Assert.AreEqual("PitchUpButton/KNF", actions.Single(a => a.Name == "PitchUpButton").Label);
    }

    [Test]
    public void Build_WithDefaults_FoldsEverySlotFromTheScheme()
    {
        // Arrange
        var defaults = Data(
            Mapping("Ship-FlightRotation", "YawLeftButton",
                primary: Bind("GenericJoystick", "Joy_4"),
                secondary: Bind("Keyboard", "Key_A")),
            Mapping("Ship-FlightRotation", "PitchUpButton", primary: Bind("Keyboard", "Key_B")),
            Mapping("Ship-FlightRotation", "YawAxisRaw", binding: Bind("GenericJoystick", "Joy_RZAxis")));
        var custom = Data(
            Mapping("Ship-FlightRotation", "YawLeftButton", primary: Bind("Keyboard", "Key_Z")));

        // Act
        var actions = EDBindingCatalogBuilder.Build(custom, defaults);

        // Assert
        var yaw = actions.Single(a => a.Name == "YawLeftButton");
        Assert.AreEqual("Keyboard-Key_A", yaw.DefaultKeyboard);
        Assert.AreEqual("GenericJoystick-Joy_4", yaw.DefaultJoystick);
        Assert.AreEqual("Keyboard-Key_Z", yaw.Keyboard);
        Assert.AreEqual("", yaw.Joystick);

        var pitch = actions.Single(a => a.Name == "PitchUpButton");
        Assert.AreEqual("Keyboard-Key_B", pitch.DefaultKeyboard);
        Assert.AreEqual("", pitch.DefaultJoystick);
        Assert.AreEqual("(Keyboard-Key_B)", pitch.Keyboard);
        Assert.AreEqual("", pitch.Joystick);

        var axis = actions.Single(a => a.Name == "YawAxisRaw");
        Assert.AreEqual("", axis.DefaultKeyboard);
        Assert.AreEqual("GenericJoystick-Joy_RZAxis", axis.DefaultJoystick);
        Assert.AreEqual("", axis.Keyboard);
        Assert.AreEqual("(GenericJoystick-Joy_RZAxis)", axis.Joystick);
    }

    [Test]
    public void Build_WithDefaults_KeepsCustomOnlyActions()
    {
        // Arrange
        var defaults = Data(Mapping("Ship-FlightRotation", "YawLeftButton", primary: Bind("Keyboard", "Key_A")));
        var custom = Data(
            Mapping("Ship-FlightRotation", "YawLeftButton", primary: Bind("Keyboard", "Key_Z")),
            Mapping("OnFoot-OnFoot", "HumanoidForwardButton", primary: Bind("Keyboard", "Key_W")));

        // Act
        var actions = EDBindingCatalogBuilder.Build(custom, defaults);

        // Assert
        Assert.AreEqual(new[] { "HumanoidForwardButton", "YawLeftButton" }, actions.Select(a => a.Name).OrderBy(n => n));
    }

    [Test]
    public void Build_WithDefaults_SetsMouseAsTheDefaultKeyboardSource()
    {
        // Arrange
        var defaults = Data(Mapping("General-InterfaceMode", "UI_Select", primary: Bind("Mouse", "Mouse_1")));

        // Act
        var action = AssertSingle(EDBindingCatalogBuilder.Build(Data(), defaults));

        // Assert
        Assert.AreEqual("mouse", action.DefaultKeyboardSource);
        Assert.AreEqual("(Mouse-Mouse_1)", action.Keyboard);
    }

    private static EDMappingData Data(params EDMapping[] mappings) => new() { Mappings = mappings };

    private static EDMapping Mapping(string group, string name, EDBinding? binding = null, EDBinding? primary = null, EDBinding? secondary = null) =>
        new(group, name) { Binding = binding, Primary = primary, Secondary = secondary };

    private static EDBinding Bind(string device, string key, params (string Device, string Key)[] modifiers) =>
        new(device, key, modifiers.Select(m => new EDBindingKey(m.Device, m.Key)).ToList());

    private static BindingAction AssertSingle(IReadOnlyList<BindingAction> actions)
    {
        Assert.AreEqual(1, actions.Count);
        return actions[0];
    }
}
