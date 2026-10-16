using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

public class JoystickBindingsFile_Tests
{
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
    public void Parse_LooksUpDescriptionByGameDeviceAndBinding()
    {
        // Act
        var descriptions = JoystickBindingsFile.Parse(Markdown);

        // Assert
        Assert.AreEqual("LT-EVO.01(FIRE)", descriptions.Get("elite_dangerous", "AAAA")["Joy_1"]);
        Assert.AreEqual("LT-EVO.01(FIRE)", descriptions.Get("star_citizen", " Left Stick {1111}")["button1"]);
        Assert.AreEqual("LT-EVO.POV(HAT_UP)", descriptions.Get("star_citizen", " Left Stick {1111}")["hat1_up"]);
        Assert.AreEqual("RT-EVO.01(FIRE)", descriptions.Get("star_citizen", " Right Stick {2222}")["button1"]);
        Assert.IsFalse(descriptions.Get("elite_dangerous", "AAAA").ContainsKey("Joy_99"));
        Assert.IsFalse(descriptions.Get("elite_dangerous", "AAAA").ContainsKey("button1"));
    }

    [Test]
    public void Parse_CapturesAnUnknownGameByItsYamlKeys()
    {
        // Arrange
        var markdown = """
            ```yaml
            STICK:
              other_sim_device_id: "XYZ"
              elite_dangerous_device_id: "AAAA"
              bindings:
                "STICK.FIRE":
                  other_sim: Axis_1
                  elite_dangerous: Joy_1
            ```
            """;

        // Act
        var descriptions = JoystickBindingsFile.Parse(markdown);

        // Assert
        Assert.AreEqual("STICK.FIRE", descriptions.Get("other_sim", "XYZ")["Axis_1"]);
        Assert.AreEqual("STICK.FIRE", descriptions.Get("elite_dangerous", "AAAA")["Joy_1"]);
        Assert.IsEmpty(descriptions.Get("star_citizen", "XYZ"));
    }

    [Test]
    public void Parse_AcceptsWindowsLineEndings()
    {
        // Act
        var descriptions = JoystickBindingsFile.Parse(Markdown.Replace("\n", "\r\n"));

        // Assert
        Assert.AreEqual("LT-EVO.01(FIRE)", descriptions.Get("star_citizen", " Left Stick {1111}")["button1"]);
    }

    [Test]
    public void Parse_ThrowsWhenThereIsNoYamlBlock()
    {
        // Act & Assert
        Assert.Throws<InvalidDataException>(() => JoystickBindingsFile.Parse("# nothing here"));
    }

    [Test]
    public void Parse_ThrowsWhenAGameBindingHasNoDeviceId()
    {
        // Arrange
        var markdown = """
            ```yaml
            LT-EVO:
              bindings:
                "LT-EVO.01(FIRE)":
                  elite_dangerous: Joy_1
            ```
            """;

        // Act & Assert
        var ex = Assert.Throws<InvalidDataException>(() => JoystickBindingsFile.Parse(markdown));
        Assert.That(ex!.Message, Does.Contain("elite_dangerous").And.Contain("LT-EVO"));
    }

    [Test]
    public void Parse_ThrowsWhenTwoNamesShareAGameBinding()
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
        Assert.Throws<InvalidDataException>(() => JoystickBindingsFile.Parse(markdown));
    }

    [Test]
    public async Task Load_ReadsTheWorkingDirectoryFile()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        Directory.CreateDirectory(root);
        try
        {
            File.WriteAllText(Path.Combine(root, BindingPrintIO.JoystickBindingsFileName), Markdown);
            var platform = new PlatformForTest(workingDir: root);

            // Act
            var descriptions = await JoystickBindingsFile.Load(platform);

            // Assert
            Assert.AreEqual("LT-EVO.01(FIRE)", descriptions.Get("star_citizen", " Left Stick {1111}")["button1"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public async Task Load_WarnsAndReturnsEmptyWhenTheFileIsMissing()
    {
        // Arrange
        var root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        Directory.CreateDirectory(root);
        try
        {
            var warnings = new List<string>();
            var platform = new PlatformForTest(workingDir: root);

            // Act
            var descriptions = await JoystickBindingsFile.Load(platform, resourceDir: Path.Combine(root, "bin"), onWarning: warnings.Add);

            // Assert
            Assert.AreSame(JoystickDescriptions.Empty, descriptions);
            Assert.AreEqual(1, warnings.Count);
            Assert.That(warnings[0], Does.Contain(BindingPrintIO.JoystickBindingsFileName).And.Contain(JoystickDescriptions.NotFound));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
