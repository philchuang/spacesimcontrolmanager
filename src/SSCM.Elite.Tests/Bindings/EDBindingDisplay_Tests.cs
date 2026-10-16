using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite.Bindings;

namespace SSCM.Elite.Tests.Bindings;

public class EDBindingDisplay_Tests
{
    [Test]
    public void Prepare_StripsKeyboardAndMouseDevicePrefixes()
    {
        // Arrange
        var document = new BindingDocument
        {
            Actions =
            [
                new BindingAction
                {
                    DefaultKeyboard = "(Keyboard-Key_Q)",
                    DefaultJoystick = "GenericJoystick-Joy_4",
                    Keyboard = "(Keyboard-Key_B); Mouse-Mouse_1 + Keyboard-Key_LeftShift",
                    Joystick = "231D0200-Joy_1 + Keyboard-Key_LeftControl",
                    JoystickDescription = "LT-EVO.01(FIRE) + Key_LeftControl",
                },
            ],
        };

        // Act
        EDBindingDisplay.Prepare(document);

        // Assert
        var action = document.Actions[0];
        Assert.AreEqual("(Key_Q)", action.DefaultKeyboard);
        Assert.AreEqual("GenericJoystick-Joy_4", action.DefaultJoystick);
        Assert.AreEqual("(Key_B); Mouse_1 + Key_LeftShift", action.Keyboard);
        Assert.AreEqual("231D0200-Joy_1 + Key_LeftControl", action.Joystick);
        Assert.AreEqual("LT-EVO.01(FIRE) + Key_LeftControl", action.JoystickDescription);
    }

    [Test]
    public void Prepare_ShowsInvertedAxesWithAnInvSuffix()
    {
        // Arrange
        var document = new BindingDocument
        {
            Actions =
            [
                new BindingAction
                {
                    DefaultJoystick = "(GenericJoystick-Neg_Joy_RZAxis)",
                    Joystick = "231D3205-Neg_Joy_RZAxis",
                },
            ],
        };

        // Act
        EDBindingDisplay.Prepare(document);

        // Assert
        var action = document.Actions[0];
        Assert.AreEqual("(GenericJoystick-Joy_RZAxis (inv))", action.DefaultJoystick);
        Assert.AreEqual("231D3205-Joy_RZAxis (inv)", action.Joystick);
    }
}
