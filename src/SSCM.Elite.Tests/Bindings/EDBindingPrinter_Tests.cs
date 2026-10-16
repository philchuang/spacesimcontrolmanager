using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using SSCM.Elite.Bindings;
using SSCM.Elite.Tests.Mocks;
using SSCM.Tests.Mocks;

namespace SSCM.Elite.Tests.Bindings;

public class EDBindingPrinter_Tests
{
    private string _root = string.Empty;
    private string _resourceDir = string.Empty;
    private string _workingDir = string.Empty;
    private string _bindsPath = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);

        this._resourceDir = Path.Combine(this._root, "bin");
        this._workingDir = Path.Combine(this._root, "work");
        Directory.CreateDirectory(this._resourceDir);
        Directory.CreateDirectory(this._workingDir);

        File.Copy(Path.Combine(AppContext.BaseDirectory, MappingImporter.ConfigFileName), Path.Combine(this._resourceDir, MappingImporter.ConfigFileName));
        File.WriteAllText(Path.Combine(this._resourceDir, EDBindingPrinter.HtmlTemplateFileName), "<html><script>const data = __BINDINGS_JSON__;</script></html>");
        File.WriteAllText(Path.Combine(this._resourceDir, EDBindingPrinter.JoystickBindingsFileName), JoystickBindings);

        var gameDir = Path.Combine(this._root, "game");
        Directory.CreateDirectory(gameDir);
        this._bindsPath = Path.Combine(gameDir, "Custom.4.0.binds");
        File.WriteAllText(this._bindsPath, CustomBindsXml);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
    }

    [Test]
    public async Task Print_All_WritesMarkdownTsvAndHtmlUnderWorkingDirectory()
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print(null);

        // Assert
        var outputDir = Path.Combine(this._workingDir, "docs", "elitedangerous", "bindings");
        Assert.AreEqual(outputDir, generator.OutputDir);
        Assert.AreEqual(2, result.ActionCount);
        Assert.AreEqual(
            new[] { "bindings.md", "bindings.tsv", "bindings.html", "bindings.json" }.Select(f => Path.Combine(outputDir, f)),
            result.WrittenPaths);
        foreach (var path in result.WrittenPaths) Assert.IsTrue(File.Exists(path), path);

        var markdown = File.ReadAllText(Path.Combine(outputDir, "bindings.md"));
        Assert.That(markdown, Does.Contain("# Elite Dangerous Bindings"));
        Assert.That(markdown, Does.Contain("|Ship-FlightRotation.YawLeftButton|"));
        Assert.That(markdown, Does.Contain("|Key_A|"));
        Assert.That(markdown, Does.Contain("231D0200-Joy_4 + 231D0200-Joy_5"));
        Assert.That(markdown, Does.Contain("LT-EVO.04(B1) + LT-EVO.05(B2)"));
        Assert.That(markdown, Does.Contain("RT-EVO.RZ(TWIST)"));
    }

    [Test]
    public async Task Print_WritesToTheSpecifiedOutputDir()
    {
        // Arrange
        var outputDir = Path.Combine(this._root, "custom-out");
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print("markdown", new BindingPrintOptions { OutputDir = outputDir });

        // Assert
        Assert.AreEqual(new[] { Path.Combine(outputDir, "bindings.md") }, result.WrittenPaths);
        Assert.IsTrue(File.Exists(Path.Combine(outputDir, "bindings.md")));
        Assert.IsFalse(Directory.Exists(Path.Combine(this._workingDir, "docs")));
    }

    [Test]
    public async Task Print_Html_WritesBindingsJsonAsASupplementalOutput()
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print("html");

        // Assert
        Assert.AreEqual(
            new[] { "bindings.html", "bindings.json" }.Select(f => Path.Combine(generator.OutputDir, f)),
            result.WrittenPaths);
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.html")), Does.Contain("YawLeftButton"));
    }

    [Test]
    public void Print_ThrowsFriendlyErrorWhenTheBindsFileIsMissing()
    {
        // Arrange
        File.Delete(this._bindsPath);
        var generator = this.CreateGenerator();

        // Act & Assert
        var ex = Assert.ThrowsAsync<SscmException>(() => generator.Print(null));
        Assert.That(ex!.Message, Does.Contain("Custom.4.0.binds"));
        Assert.IsFalse(Directory.Exists(generator.OutputDir));
    }

    [Test]
    public async Task Print_WithRefresh_WarnsAndStillPrints()
    {
        // Arrange
        var generator = this.CreateGenerator();
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        var result = await generator.Print("markdown", new BindingPrintOptions { Flags = [EDBindingPrinter.RefreshDefaultsFlag.Name] });

        // Assert
        Assert.AreEqual(2, result.ActionCount);
        Assert.That(warnings, Has.Some.Contain("not implemented"));
        Assert.IsTrue(File.Exists(Path.Combine(generator.OutputDir, "bindings.md")));
    }

    [Test]
    public async Task Print_UsesControlSchemeDefaultsAndLocalizationWhenTheyArePresent()
    {
        // Arrange
        var schemes = Path.Combine(this._root, "install", EDFolders.CONTROL_SCHEMES_DIR);
        Directory.CreateDirectory(schemes);
        File.WriteAllText(Path.Combine(schemes, EDFolders.DEFAULT_BINDS + EDFolders.BINDS_EXTENSION), GenericJoystickDefaultsXml);
        File.WriteAllText(Path.Combine(this._workingDir, EDLocalizationFileSource.FileName), "YawLeftButton=Yaw Left\nPitchUpButton=Pitch Up\n");
        var generator = this.CreateGenerator(gameRoot: Path.Combine(this._root, "install"));

        // Act
        var result = await generator.Print(null);

        // Assert
        Assert.AreEqual(3, result.ActionCount);
        var markdown = File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md"));
        Assert.That(markdown, Does.Contain("|Yaw Left|"));
        Assert.That(markdown, Does.Contain("|Pitch Up|"));
        Assert.That(markdown, Does.Contain("|(Key_B)|"));

        var json = File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.json"));
        Assert.That(json, Does.Contain("\"defaultKeyboard\": \"Key_Q\""));
        Assert.That(json, Does.Contain("\"defaultJoystick\": \"GenericJoystick-Joy_RZAxis\""));
    }

    [Test]
    public void Flags_ListsTheRefreshSwitch()
    {
        // Act & Assert
        Assert.AreEqual(new[] { "refresh-defaults" }, EDBindingPrinter.Flags.Select(f => f.Name));
    }

    [Test]
    public void Print_ThrowsFriendlyErrorListingTheFormatsWhenTheFormatIsUnknown()
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act & Assert
        var ex = Assert.ThrowsAsync<SscmException>(() => generator.Print("pdf"));
        Assert.That(ex!.Message, Does.Contain("[pdf]").And.Contain("markdown, tsv, html"));
    }

    private EDBindingPrinter CreateGenerator(string? gameRoot = null)
    {
        var folders = new EDFoldersForTest
        {
            GameRoot = gameRoot ?? Path.Combine(this._root, "install"),
            GameConfigDir = Path.GetDirectoryName(this._bindsPath)!,
            GameConfigPath = this._bindsPath,
            EliteDataDir = Path.Combine(this._root, "data"),
        };
        var platform = new PlatformForTest(utcnow: new DateTime(2026, 10, 15, 16, 30, 0, DateTimeKind.Utc), workingDir: this._workingDir);
        return new EDBindingPrinter(platform, folders, resourceDir: this._resourceDir);
    }

    private const string CustomBindsXml = """
        <Root PresetName="Custom" MajorVersion="4" MinorVersion="0">
          <YawLeftButton>
            <Primary Device="Keyboard" Key="Key_A" />
            <Secondary Device="231D0200" Key="Joy_4">
              <Modifier Device="231D0200" Key="Joy_5" />
            </Secondary>
          </YawLeftButton>
          <YawAxisRaw>
            <Binding Device="231D3205" Key="Joy_RZAxis" />
          </YawAxisRaw>
        </Root>
        """;

    private const string GenericJoystickDefaultsXml = """
        <Root PresetName="GenericJoystick">
          <YawLeftButton>
            <Primary Device="GenericJoystick" Key="Joy_4" />
            <Secondary Device="Keyboard" Key="Key_Q" />
          </YawLeftButton>
          <PitchUpButton>
            <Primary Device="Keyboard" Key="Key_B" />
          </PitchUpButton>
          <YawAxisRaw>
            <Binding Device="GenericJoystick" Key="Joy_RZAxis" />
          </YawAxisRaw>
        </Root>
        """;

    private const string JoystickBindings = """
        # Joystick binding names

        ```yaml
        LT-EVO:
          elite_dangerous_device_id: "231D0200"
          bindings:
            "LT-EVO.04(B1)":
              elite_dangerous: Joy_4
            "LT-EVO.05(B2)":
              elite_dangerous: Joy_5
        RT-EVO:
          elite_dangerous_device_id: "231D3205"
          bindings:
            "RT-EVO.RZ(TWIST)":
              elite_dangerous: Joy_RZAxis
        ```
        """;
}
