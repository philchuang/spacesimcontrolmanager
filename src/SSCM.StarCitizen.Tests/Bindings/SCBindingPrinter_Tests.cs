using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SSCM.Core;
using SSCM.StarCitizen.Bindings;
using SSCM.StarCitizen.Tests.Mocks;
using SSCM.Tests.Mocks;

namespace SSCM.StarCitizen.Tests.Bindings;

public class SCBindingPrinter_Tests
{
    private string _root = string.Empty;
    private string _environmentDir = string.Empty;
    private string _resourceDir = string.Empty;
    private string _workingDir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);

        this._environmentDir = Path.Combine(this._root, "StarCitizen", "LIVE");
        this._resourceDir = Path.Combine(this._root, "bin");
        this._workingDir = Path.Combine(this._root, "work");
        Directory.CreateDirectory(this._resourceDir);
        Directory.CreateDirectory(this._workingDir);

        File.WriteAllText(this.ShippedProfilePath, DefaultProfileXml);
        File.WriteAllText(Path.Combine(this._resourceDir, SCBindingPrinter.JoystickBindingsFileName), JoystickBindings);
        File.WriteAllText(Path.Combine(this._resourceDir, SCBindingPrinter.HtmlTemplateFileName), "<html><script>const data = __BINDINGS_JSON__;</script></html>");

        var localization = Path.Combine(this._environmentDir, "data", "Localization", "english");
        Directory.CreateDirectory(localization);
        File.WriteAllText(Path.Combine(localization, "global.ini"), "ui_seat=Seat Controls\nui_fire=Fire Weapons\n");

        File.WriteAllText(this.ActionMapsPath, ActionMapsXml);
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
        var outputDir = Path.Combine(this._workingDir, "docs", "starcitizen", "bindings");
        Assert.AreEqual(outputDir, generator.OutputDir);
        Assert.AreEqual(2, result.ActionCount);
        Assert.AreEqual(
            new[] { "bindings.md", "bindings.tsv", "bindings.html", "bindings.json" }.Select(f => Path.Combine(outputDir, f)),
            result.WrittenPaths);
        foreach (var path in result.WrittenPaths) Assert.IsTrue(File.Exists(path), path);

        var markdown = File.ReadAllText(Path.Combine(outputDir, "bindings.md"));
        Assert.That(markdown, Does.Contain("|seat-fire|Seat Controls|Fire Weapons|kb1_f|js1_button1 2x|LT-EVO.01(FIRE)|"));
        Assert.That(markdown, Does.Contain("|seat-look|Seat Controls|Look|mo1_mouse1|||"));

        var html = File.ReadAllText(Path.Combine(outputDir, "bindings.html"));
        Assert.That(html, Does.Contain("\"generatedAt\":\"2026-10-15T"));
        Assert.That(html, Does.Contain("\"joystickDesc\":\"LT-EVO.01(FIRE)\""));
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
        Assert.AreEqual(Path.Combine(this._workingDir, "docs", "starcitizen", "bindings"), generator.OutputDir, "the default location is unchanged");
        Assert.AreEqual(new[] { Path.Combine(outputDir, "bindings.md") }, result.WrittenPaths);
        Assert.IsTrue(File.Exists(Path.Combine(outputDir, "bindings.md")));
        Assert.IsFalse(Directory.Exists(Path.Combine(this._workingDir, "docs")));
    }

    [Test]
    public async Task Print_ResolvesARelativeOutputDirAgainstTheWorkingDirectory()
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print("markdown", new BindingPrintOptions { OutputDir = Path.Combine("out", "bindings") });

        // Assert
        var outputDir = Path.Combine(this._workingDir, "out", "bindings");
        Assert.AreEqual(new[] { Path.Combine(outputDir, "bindings.md") }, result.WrittenPaths);
        Assert.IsTrue(File.Exists(Path.Combine(outputDir, "bindings.md")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task Print_UsesTheDefaultOutputDirWhenNoneIsGiven(string? outputDir)
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print("markdown", new BindingPrintOptions { OutputDir = outputDir });

        // Assert
        Assert.AreEqual(new[] { Path.Combine(generator.OutputDir, "bindings.md") }, result.WrittenPaths);
    }

    private static readonly BindingPrintOptions RefreshDefaults = new() { Flags = [SCBindingPrinter.RefreshDefaultsFlag.Name] };

    [Test]
    public void Formats_ListsEveryOutputThatCanBePrintedOnItsOwn()
    {
        // Act & Assert
        Assert.AreEqual(new[] { "markdown", "tsv", "html" }, SCBindingPrinter.Formats.Select(f => f.Name));
        Assert.IsTrue(SCBindingPrinter.Formats.All(f => !string.IsNullOrWhiteSpace(f.Description)));
    }

    [Test]
    public void Flags_ListsTheRefreshSwitch()
    {
        // Act & Assert
        Assert.AreEqual(new[] { "refresh-defaults" }, SCBindingPrinter.Flags.Select(f => f.Name));
        Assert.IsFalse(string.IsNullOrWhiteSpace(SCBindingPrinter.Flags.Single().Description));
    }

    [TestCase("markdown")]
    [TestCase("Markdown")]
    [TestCase("  MARKDOWN ")]
    public async Task Print_MatchesFormatNamesIgnoringCaseAndSurroundingSpaces(string format)
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print(format);

        // Assert
        Assert.AreEqual(new[] { Path.Combine(generator.OutputDir, "bindings.md") }, result.WrittenPaths);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task Print_TreatsAMissingFormatAsEveryFormat(string? format)
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print(format);

        // Assert
        Assert.AreEqual(4, result.WrittenPaths.Count);
    }

    [Test]
    public void Print_ThrowsFriendlyErrorListingTheFormatsWhenTheFormatIsUnknown()
    {
        // Arrange
        var generator = this.CreateGenerator(new FakeProfileSource { Profile = null });

        // Act & Assert
        var ex = Assert.ThrowsAsync<SscmException>(() => generator.Print("pdf", RefreshDefaults));
        Assert.That(ex!.Message, Does.Contain("[pdf]").And.Contain("markdown, tsv, html"));
        Assert.IsFalse(Directory.Exists(generator.OutputDir), "nothing is written");
        Assert.IsFalse(File.Exists(this.WorkingProfilePath), "the refresh does not run for a bad format");
    }

    [Test]
    public async Task Print_IgnoresFlagsItDoesNotKnow()
    {
        // Arrange
        var generator = this.CreateGenerator(new FakeProfileSource { Profile = null });

        // Act
        var result = await generator.Print("markdown", new BindingPrintOptions { Flags = ["not-a-flag"] });

        // Assert
        Assert.AreEqual(2, result.ActionCount);
    }

    [Test]
    public async Task Print_MatchesFlagNamesIgnoringCase()
    {
        // Arrange
        var generator = this.CreateGenerator(new FakeProfileSource());

        // Act
        await generator.Print("markdown", new BindingPrintOptions { Flags = ["REFRESH-DEFAULTS"] });

        // Assert
        Assert.IsTrue(File.Exists(this.WorkingProfilePath));
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

        var json = JObject.Parse(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.json")));
        var fire = ((JArray) json["actions"]!).Cast<JObject>().Single(a => (string?) a["id"] == "seat-fire");
        Assert.AreEqual("seat", (string?) fire["categoryName"]);
        Assert.AreEqual("Seat Controls", (string?) fire["categoryLabel"]);
        Assert.AreEqual("fire", (string?) fire["name"]);
        Assert.AreEqual("Fire Weapons", (string?) fire["label"]);
        Assert.AreEqual("space", (string?) fire["defaultKeyboard"]);
        Assert.AreEqual("keyboard", (string?) fire["defaultKeyboardSource"]);
        Assert.AreEqual("kb1_f", (string?) fire["keyboard"]);
        Assert.AreEqual("js1_button1 2x", (string?) fire["joystick"]);
        Assert.AreEqual("LT-EVO.01(FIRE)", (string?) fire["joystickDesc"]);
    }

    [TestCase("markdown", "bindings.md")]
    [TestCase("tsv", "bindings.tsv")]
    public async Task Print_SingleFormat_WritesOnlyThatFile(string format, string expectedFile)
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var result = await generator.Print(format);

        // Assert
        Assert.AreEqual(new[] { Path.Combine(generator.OutputDir, expectedFile) }, result.WrittenPaths);
        Assert.AreEqual(new[] { expectedFile }, Directory.GetFiles(generator.OutputDir).Select(Path.GetFileName));
    }

    [Test]
    public async Task Print_ReportsWhatItWrote()
    {
        // Arrange
        var generator = this.CreateGenerator();
        var messages = new List<string>();
        generator.StandardOutput += messages.Add;

        // Act
        await generator.Print("tsv");

        // Assert
        Assert.That(messages, Has.Some.Contain("Reading").And.Contain("actionmaps.xml"));
        Assert.That(messages, Has.Some.Contain("2 TSV rows").And.Contain("bindings.tsv"));
    }

    [Test]
    public async Task BuildDocument_FormatsGeneratedAtAsLocalIsoWithOffset()
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        var document = await generator.BuildDocument();

        // Assert
        Assert.That(document.GeneratedAt, Does.Match(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$"));
        Assert.AreEqual(
            new DateTimeOffset(new DateTime(2026, 10, 15, 16, 30, 0, DateTimeKind.Utc)).ToLocalTime().ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            document.GeneratedAt);
    }

    [Test]
    public async Task Print_WithoutRefresh_ReadsTheExecutableDirectoryProfileAndNeverTouchesTheGame()
    {
        // Arrange
        var refresh = new FakeProfileSource { Profile = null };
        var generator = this.CreateGenerator(refresh);

        // Act
        var result = await generator.Print("markdown", BindingPrintOptions.Default);

        // Assert
        Assert.AreEqual(0, refresh.LoadCount);
        Assert.AreEqual(2, result.ActionCount);
        Assert.AreEqual(DefaultProfileXml, File.ReadAllText(this.ShippedProfilePath), "the profile next to the executable is not modified");
        Assert.IsFalse(File.Exists(this.WorkingProfilePath), "printing never writes a profile");
    }

    [Test]
    public async Task Print_PrefersTheWorkingDirectoryProfileOverTheExecutableDirectoryOne()
    {
        // Arrange
        File.WriteAllText(this.WorkingProfilePath, """
            <profile>
              <actionmap name="seat" UILabel="@ui_seat">
                <action name="boost" UILabel="Boost" keyboard="b" />
              </actionmap>
            </profile>
            """);
        var generator = this.CreateGenerator(new FakeProfileSource { Profile = null });

        // Act
        var result = await generator.Print("markdown");

        // Assert
        Assert.AreEqual(1, result.ActionCount);
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-boost|"));
    }

    [Test]
    public async Task Print_FallsBackToTheExecutableDirectoryProfileWhenTheWorkingDirectoryHasNone()
    {
        // Arrange
        var generator = this.CreateGenerator(new FakeProfileSource { Profile = null });

        // Act
        var result = await generator.Print("markdown");

        // Assert
        Assert.IsFalse(File.Exists(this.WorkingProfilePath));
        Assert.AreEqual(2, result.ActionCount);
    }

    [Test]
    public async Task Print_WithRefresh_SavesTheProfileToTheWorkingDirectoryThenPrintsFromIt()
    {
        // Arrange
        var refreshed = XDocument.Parse("""
            <profile>
              <actionmap name="seat" UILabel="@ui_seat">
                <action name="boost" UILabel="Boost" keyboard="b" />
              </actionmap>
            </profile>
            """);
        var refresh = new FakeProfileSource { Profile = refreshed };
        var generator = this.CreateGenerator(refresh);
        var messages = new List<string>();
        generator.StandardOutput += messages.Add;

        // Act
        var result = await generator.Print("markdown", RefreshDefaults);

        // Assert
        Assert.AreEqual(1, refresh.LoadCount);
        Assert.AreEqual(1, result.ActionCount, "the printed document comes from the refreshed profile");
        var onDisk = XDocument.Parse(File.ReadAllText(this.WorkingProfilePath));
        Assert.AreEqual("boost", (string?) onDisk.Descendants("action").Single().Attribute("name"));
        Assert.AreEqual(DefaultProfileXml, File.ReadAllText(this.ShippedProfilePath), "the profile next to the executable is left alone");
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-boost|Seat Controls|Boost|(b)|||"));
        Assert.That(messages.First(), Does.Contain("Refreshed default profile").And.Contain(this.WorkingProfilePath));
    }

    [Test]
    public async Task Print_WithRefresh_OverwritesAnExistingWorkingDirectoryProfile()
    {
        // Arrange
        File.WriteAllText(this.WorkingProfilePath, "<profile><actionmap name=\"stale\" /></profile>");
        var generator = this.CreateGenerator(new FakeProfileSource());

        // Act
        var result = await generator.Print("markdown", RefreshDefaults);

        // Assert
        Assert.AreEqual(2, result.ActionCount);
        Assert.That(File.ReadAllText(this.WorkingProfilePath), Does.Not.Contain("stale"));
    }

    [Test]
    public async Task Print_WithRefresh_WorksWhenTheExecutableDirectoryHasNoProfile()
    {
        // Arrange
        File.Delete(this.ShippedProfilePath);
        var generator = this.CreateGenerator(new FakeProfileSource());

        // Act
        var result = await generator.Print("markdown", RefreshDefaults);

        // Assert
        Assert.IsTrue(File.Exists(this.WorkingProfilePath));
        Assert.AreEqual(2, result.ActionCount);
    }

    [Test]
    public void Print_WithRefresh_WritesNothingWhenExtractionFails()
    {
        // Arrange
        var refresh = new FakeProfileSource { Exception = new FileNotFoundException("no archive") };
        var generator = this.CreateGenerator(refresh);

        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(() => generator.Print(null, RefreshDefaults));
        Assert.AreEqual(DefaultProfileXml, File.ReadAllText(this.ShippedProfilePath));
        Assert.IsFalse(File.Exists(this.WorkingProfilePath));
        Assert.IsFalse(Directory.Exists(generator.OutputDir), "nothing is printed after a failed refresh");
    }

    [Test]
    public void Print_ThrowsWithRefreshHintWhenNeitherDirectoryHasAProfile()
    {
        // Arrange
        File.Delete(this.ShippedProfilePath);
        var generator = this.CreateGenerator();

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => generator.Print(null));
        Assert.That(ex!.Message, Does.Contain(this.WorkingProfilePath).And.Contain(this.ShippedProfilePath).And.Contain("--refresh-defaults"));
    }

    [Test]
    public void Print_ThrowsWhenTheHtmlTemplateIsMissing()
    {
        // Arrange
        File.Delete(Path.Combine(this._resourceDir, "star-citizen-bindings.template.html"));
        var generator = this.CreateGenerator();

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => generator.Print("html"));
        Assert.That(ex!.Message, Does.Contain("star-citizen-bindings.template.html")
            .And.Contain(Path.Combine(this._workingDir, "star-citizen-bindings.template.html"))
            .And.Contain(Path.Combine(this._resourceDir, "star-citizen-bindings.template.html")));
    }

    [Test]
    public async Task Print_Html_PrefersAnHtmlTemplateInTheWorkingDirectory()
    {
        // Arrange
        File.WriteAllText(Path.Combine(this._workingDir, "star-citizen-bindings.template.html"), "<html>MY TEMPLATE __BINDINGS_JSON__</html>");
        var generator = this.CreateGenerator();

        // Act
        await generator.Print("html");

        // Assert
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.html")), Does.StartWith("<html>MY TEMPLATE "));
        Assert.That(File.ReadAllText(Path.Combine(this._resourceDir, "star-citizen-bindings.template.html")), Does.Not.Contain("MY TEMPLATE"), "the shipped template is untouched");
    }

    [Test]
    public async Task Print_Html_FallsBackToTheShippedHtmlTemplate()
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        await generator.Print("html");

        // Assert
        Assert.IsFalse(File.Exists(Path.Combine(this._workingDir, "star-citizen-bindings.template.html")));
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.html")), Does.StartWith("<html><script>const data = "));
    }

    [Test]
    public async Task Print_PrefersJoystickBindingsInTheWorkingDirectory()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(this._workingDir, "joystick-bindings.md"),
            JoystickBindings.Replace("LT-EVO.01(FIRE)", "MY-STICK.01(TRIGGER)"));
        var generator = this.CreateGenerator();
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        await generator.Print("markdown");

        // Assert
        Assert.IsEmpty(warnings);
        var markdown = File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md"));
        Assert.That(markdown, Does.Contain("|js1_button1 2x|MY-STICK.01(TRIGGER)|"));
        Assert.That(markdown, Does.Not.Contain("LT-EVO.01(FIRE)"), "the shipped file is ignored when the working directory has one");
    }

    [Test]
    public async Task Print_UsesTheShippedJoystickBindingsWhenTheWorkingDirectoryHasNone()
    {
        // Arrange
        var generator = this.CreateGenerator();

        // Act
        await generator.Print("markdown");

        // Assert
        Assert.IsFalse(File.Exists(Path.Combine(this._workingDir, "joystick-bindings.md")));
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|js1_button1 2x|LT-EVO.01(FIRE)|"));
    }

    [Test]
    public async Task Print_WithoutJoystickBindingsFile_WarnsAndMarksBoundJoystickInputsNotFound()
    {
        // Arrange
        File.Delete(Path.Combine(this._resourceDir, "joystick-bindings.md"));
        var generator = this.CreateGenerator();
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        var result = await generator.Print("markdown");

        // Assert
        Assert.AreEqual(2, result.ActionCount);
        Assert.AreEqual(1, warnings.Count);
        Assert.That(warnings[0], Does.Contain("joystick-bindings.md").And.Contain("{NOT_FOUND}")
            .And.Contain(Path.Combine(this._workingDir, "joystick-bindings.md"))
            .And.Contain(Path.Combine(this._resourceDir, "joystick-bindings.md")));
        var markdown = File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md"));
        Assert.That(markdown, Does.Contain("|seat-fire|Seat Controls|Fire Weapons|kb1_f|js1_button1 2x|{NOT_FOUND}|"));
        Assert.That(markdown, Does.Contain("|seat-look|Seat Controls|Look|mo1_mouse1|||"), "mouse rebinds fill the keyboard column and have nothing to describe");
    }

    [Test]
    public async Task Print_WhenAJoystickDeviceIsNotInActionMaps_WarnsAndMarksItsInputsNotFound()
    {
        // Arrange
        File.WriteAllText(
            Path.Combine(this._resourceDir, "joystick-bindings.md"),
            JoystickBindings.Replace("Test Stick {1234}", "Some Other Stick {9999}"));
        var generator = this.CreateGenerator();
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        await generator.Print("markdown");

        // Assert
        Assert.AreEqual(2, warnings.Count);
        Assert.That(warnings[0], Does.Contain("Some Other Stick {9999}"));
        Assert.That(warnings[1], Does.Contain(" Test Stick {1234}").And.Contain("joystick-bindings.md"));
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|js1_button1 2x|{NOT_FOUND}|"));
    }

    [Test]
    public void Print_ThrowsFriendlyErrorListingInstalledEnvironmentsWhenTheEnvironmentIsMissing()
    {
        // Arrange
        var gameRoot = Path.Combine(this._root, "StarCitizen");
        Directory.CreateDirectory(Path.Combine(gameRoot, "HOTFIX"));
        File.WriteAllText(Path.Combine(gameRoot, "HOTFIX", "Data.p4k"), "");
        File.WriteAllText(Path.Combine(this._environmentDir, "Data.p4k"), "");
        Directory.CreateDirectory(Path.Combine(gameRoot, "NotAnEnvironment"));
        var folders = new SCFoldersForTest { Environment = "ptu", GameEnvironmentDir = Path.Combine(gameRoot, "PTU") };
        var generator = this.CreateGenerator(folders: folders);

        // Act & Assert
        var ex = Assert.ThrowsAsync<SscmException>(() => generator.Print(null, RefreshDefaults));
        Assert.That(ex!.Message, Does.Contain("[PTU]").And.Contain(Path.Combine(gameRoot, "PTU")));
        Assert.That(ex.Message, Does.Contain("Installed environments: HOTFIX, LIVE."));
        Assert.That(ex.Message, Does.Contain("--environment"));
        Assert.That(ex.Message, Does.Not.Contain("NotAnEnvironment"));
        Assert.IsFalse(Directory.Exists(generator.OutputDir));
    }

    [Test]
    public void Print_ThrowsFriendlyErrorWhenNoEnvironmentsAreInstalled()
    {
        // Arrange
        var gameRoot = Path.Combine(this._root, "StarCitizen");
        var folders = new SCFoldersForTest { Environment = "live", GameEnvironmentDir = Path.Combine(gameRoot, "LIVE-MISSING") };
        var generator = this.CreateGenerator(folders: folders);

        // Act & Assert
        var ex = Assert.ThrowsAsync<SscmException>(() => generator.Print(null));
        Assert.That(ex!.Message, Does.Contain("[LIVE]").And.Contain("No installed environments"));
    }

    [Test]
    public void Print_ThrowsFriendlyErrorWhenStarCitizenIsNotInstalled()
    {
        // Arrange
        var missingRoot = Path.Combine(this._root, "NoStarCitizenHere");
        var folders = new SCFoldersForTest { GameEnvironmentDir = Path.Combine(missingRoot, "LIVE") };
        var generator = this.CreateGenerator(folders: folders);

        // Act & Assert
        var ex = Assert.ThrowsAsync<SscmException>(() => generator.Print(null));
        Assert.That(ex!.Message, Does.Contain("Star Citizen was not found").And.Contain(missingRoot).And.Contain("SCFolders:GameRoot"));
    }

    [Test]
    public void Print_ThrowsFriendlyErrorWhenTheGameHasNoUserSettings()
    {
        // Arrange: a real SCFolders over an installed environment that has never created its USER folder.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SCFolders:GameRoot"] = Path.Combine(this._root, "StarCitizen"),
                ["SCFolders:ScDataDir"] = ".",
            })
            .Build();
        var folders = new SCFolders(new PlatformForTest(), new TestSscmFolders(), config);
        var generator = new SCBindingPrinter(new PlatformForTest(workingDir: this._workingDir), folders, new FakeProfileSource(), this._resourceDir);
        Directory.Delete(Path.Combine(this._environmentDir, "USER"), recursive: true);

        // Act & Assert
        var ex = Assert.ThrowsAsync<SscmException>(() => generator.Print(null));
        Assert.That(ex!.Message, Does.Contain("game config directory").And.Contain("Start Star Citizen once"));
    }

    [Test]
    public async Task Print_WithoutLocalizationFile_WarnsAndMarksLocalizedLabelsFileNotFound()
    {
        // Arrange
        File.Delete(this.LocalizationPath);
        var generator = this.CreateGenerator();
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        var result = await generator.Print(null);

        // Assert
        Assert.AreEqual(2, result.ActionCount);
        Assert.AreEqual(1, warnings.Count);
        Assert.That(warnings[0], Does.Contain("/FNF")
            .And.Contain(Path.Combine(this._workingDir, "global.ini"))
            .And.Contain(this.LocalizationPath)
            .And.Contain("--refresh-defaults"));
        var markdown = File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md"));
        Assert.That(markdown, Does.Contain("|seat-fire|@ui_seat/FNF|@ui_fire/FNF|"));
    }

    [Test]
    public async Task Print_AlwaysUsesTheGameLocalizationWhenItExists()
    {
        // Arrange
        File.WriteAllText(this.WorkingLocalizationPath, "ui_seat=Working Seats\nui_fire=Working Fire\n");
        var generator = this.CreateGenerator();

        // Act
        await generator.Print("markdown");

        // Assert
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-fire|Seat Controls|Fire Weapons|"));
        Assert.That(File.ReadAllText(this.WorkingLocalizationPath), Does.Contain("Working Seats"), "the working directory copy is not modified");
    }

    [Test]
    public async Task Print_UsesTheWorkingDirectoryLocalizationWhenTheGameHasNone()
    {
        // Arrange
        File.Delete(this.LocalizationPath);
        File.WriteAllText(this.WorkingLocalizationPath, "ui_seat=Working Seats\nui_fire=Working Fire\n");
        var generator = this.CreateGenerator();
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        await generator.Print("markdown");

        // Assert
        Assert.IsEmpty(warnings);
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-fire|Working Seats|Working Fire|"));
    }

    [Test]
    public async Task Print_WithRefresh_WhenTheGameHasNoLocalization_ExtractsItWithTheProfileInOnePass()
    {
        // Arrange
        File.Delete(this.LocalizationPath);
        var refresh = new FakeProfileSource { Localization = Encoding.UTF8.GetBytes("ui_seat=Archive Seats\nui_fire=Archive Fire\n") };
        var generator = this.CreateGenerator(refresh);
        var messages = new List<string>();
        generator.StandardOutput += messages.Add;

        // Act
        await generator.Print("markdown", RefreshDefaults);

        // Assert
        Assert.AreEqual(1, refresh.LoadCount, "the profile and the localization come from a single extraction");
        Assert.AreEqual(new[] { true }, refresh.LocalizationRequests);
        Assert.AreEqual(Encoding.UTF8.GetBytes("ui_seat=Archive Seats\nui_fire=Archive Fire\n"), File.ReadAllBytes(this.WorkingLocalizationPath));
        Assert.IsFalse(File.Exists(this.LocalizationPath), "nothing is written into the game folder");
        Assert.IsFalse(File.Exists(this.WorkingLocalizationPath + ".tmp"));
        Assert.IsTrue(File.Exists(this.WorkingProfilePath));
        Assert.That(messages, Has.Some.Contain("Extracted localization").And.Contain(this.WorkingLocalizationPath));
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-fire|Archive Seats|Archive Fire|"));
    }

    [Test]
    public async Task Print_WithRefresh_WhenTheGameHasLocalization_DoesNotExtractIt()
    {
        // Arrange
        var refresh = new FakeProfileSource();
        var generator = this.CreateGenerator(refresh);

        // Act
        await generator.Print("markdown", RefreshDefaults);

        // Assert
        Assert.AreEqual(new[] { false }, refresh.LocalizationRequests);
        Assert.IsFalse(File.Exists(this.WorkingLocalizationPath));
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-fire|Seat Controls|Fire Weapons|"));
    }

    [Test]
    public async Task Print_WithRefresh_OverwritesAStaleWorkingDirectoryLocalizationWhenTheGameHasNone()
    {
        // Arrange
        File.Delete(this.LocalizationPath);
        File.WriteAllText(this.WorkingLocalizationPath, "ui_seat=Stale\n");
        var generator = this.CreateGenerator(new FakeProfileSource());

        // Act
        await generator.Print("markdown", RefreshDefaults);

        // Assert
        Assert.That(File.ReadAllText(this.WorkingLocalizationPath), Does.Contain("Archive Seat").And.Not.Contain("Stale"));
    }

    [Test]
    public async Task Print_WithRefresh_WhenTheArchiveHasNoLocalization_WarnsAndStillPrints()
    {
        // Arrange
        File.Delete(this.LocalizationPath);
        var generator = this.CreateGenerator(new FakeProfileSource { Localization = null });
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        var result = await generator.Print("markdown", RefreshDefaults);

        // Assert
        Assert.AreEqual(2, result.ActionCount);
        Assert.IsFalse(File.Exists(this.WorkingLocalizationPath));
        Assert.That(warnings, Has.Some.Contain("Data/Localization/english/global.ini"));
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("@ui_seat/FNF"));
    }

    [Test]
    public async Task Print_WhenAKeyIsMissingFromTheLocalizationFile_AppendsKnFSuffix()
    {
        // Arrange
        File.WriteAllText(this.LocalizationPath, "ui_seat=Seat Controls\n");
        var generator = this.CreateGenerator();
        var warnings = new List<string>();
        generator.WarningOutput += warnings.Add;

        // Act
        await generator.Print("markdown");

        // Assert
        Assert.IsEmpty(warnings);
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-fire|Seat Controls|@ui_fire/KNF|"));
    }

    [Test]
    public async Task Print_ReadsLocalizationFromThePathTheFoldersProvide()
    {
        // Arrange
        var elsewhere = Path.Combine(this._root, "elsewhere", "strings.ini");
        Directory.CreateDirectory(Path.GetDirectoryName(elsewhere)!);
        File.WriteAllText(elsewhere, "ui_seat=Custom Seats\nui_fire=Custom Fire\n");
        File.Delete(this.LocalizationPath);
        var folders = new SCFoldersForTest
        {
            GameEnvironmentDir = this._environmentDir,
            GameMappingsPath = this.ActionMapsPath,
            GameLocalizationPath = elsewhere,
        };
        var generator = this.CreateGenerator(folders: folders);

        // Act
        await generator.Print("markdown");

        // Assert
        Assert.That(File.ReadAllText(Path.Combine(generator.OutputDir, "bindings.md")), Does.Contain("|seat-fire|Custom Seats|Custom Fire|"));
    }

    [Test]
    public void Print_ThrowsWhenActionMapsFileIsMissing()
    {
        // Arrange
        File.Delete(this.ActionMapsPath);
        var generator = this.CreateGenerator();

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => generator.Print(null));
        Assert.That(ex!.Message, Does.Contain("actionmaps.xml"));
    }

    [Test]
    public void P4kProfileSource_ThrowsWhenDataArchiveIsMissing()
    {
        // Arrange
        var source = new SCP4kGameDataSource(new SCFoldersForTest { GameEnvironmentDir = this._environmentDir });

        // Act & Assert
        var ex = Assert.ThrowsAsync<FileNotFoundException>(() => source.Load());
        Assert.That(ex!.Message, Does.Contain("Data.p4k"));
    }

    /// <summary>The game's own copy.</summary>
    private string LocalizationPath => Path.Combine(this._environmentDir, "data", "Localization", "english", "global.ini");

    /// <summary>Where --refresh-defaults saves an extracted copy. It is only read when the game has no copy.</summary>
    private string WorkingLocalizationPath => Path.Combine(this._workingDir, "global.ini");

    private string ActionMapsPath => Path.Combine(this._environmentDir, "USER", "Client", "0", "Profiles", "default", "actionmaps.xml").EnsureDirectory();

    /// <summary>The copy the build puts next to the executable.</summary>
    private string ShippedProfilePath => Path.Combine(this._resourceDir, "defaultProfile.xml");

    /// <summary>Where --refresh-defaults saves, and the first place the profile is read from.</summary>
    private string WorkingProfilePath => Path.Combine(this._workingDir, "defaultProfile.xml");

    private SCBindingPrinter CreateGenerator(FakeProfileSource? refreshSource = null, SCFoldersForTest? folders = null)
    {
        folders ??= new SCFoldersForTest { GameEnvironmentDir = this._environmentDir, GameMappingsPath = this.ActionMapsPath, GameLocalizationPath = this.LocalizationPath };
        var platform = new PlatformForTest(utcnow: new DateTime(2026, 10, 15, 16, 30, 0, DateTimeKind.Utc), workingDir: this._workingDir);
        return new SCBindingPrinter(platform, folders, refreshSource ?? new FakeProfileSource(), this._resourceDir);
    }

    /// <summary>Stands in for the Data.p4k extraction that --refresh-defaults performs.</summary>
    private class FakeProfileSource : ISCGameDataSource
    {
        public XDocument? Profile { get; set; } = XDocument.Parse(DefaultProfileXml);

        /// <summary>What the archive holds for global.ini. It is only handed out when the generator asks for it.</summary>
        public byte[]? Localization { get; set; } = Encoding.UTF8.GetBytes("ui_seat=Archive Seat\nui_fire=Archive Fire\n");

        public Exception? Exception { get; set; }

        /// <summary>The number of extractions. Each one stands for a single pass over the archive.</summary>
        public int LoadCount { get; private set; }

        /// <summary>The <c>includeLocalization</c> argument of each extraction.</summary>
        public List<bool> LocalizationRequests { get; } = new();

        public Task<SCGameData> Extract(bool includeLocalization, CancellationToken cancellationToken = default)
        {
            this.LoadCount++;
            this.LocalizationRequests.Add(includeLocalization);
            if (this.Exception != null) throw this.Exception;
            var profile = this.Profile ?? throw new InvalidOperationException("The refresh source must not be used.");
            return Task.FromResult(new SCGameData(profile, includeLocalization ? this.Localization : null));
        }
    }

    private const string DefaultProfileXml = """
        <profile>
          <actionmap name="seat" UILabel="@ui_seat">
            <action name="fire" UILabel="@ui_fire" keyboard="space" joystick=" " />
            <action name="look" UILabel="Look" mouse="maxis_x" />
          </actionmap>
        </profile>
        """;

    private const string ActionMapsXml = """
        <ActionMaps>
          <ActionProfiles profileName="default">
            <options type="joystick" instance="1" Product=" Test Stick {1234}" />
            <actionmap name="seat">
              <action name="fire">
                <rebind input="kb1_f" />
                <rebind input="js1_button1" multiTap="2" />
              </action>
              <action name="look">
                <rebind input="mo1_mouse1" />
              </action>
            </actionmap>
          </ActionProfiles>
        </ActionMaps>
        """;

    private const string JoystickBindings = """
        # Joystick binding names

        ```yaml
        LT-EVO:
          star_citizen_device_id: " Test Stick {1234}"
          bindings:
            "LT-EVO.01(FIRE)":
              star_citizen: button1
        ```
        """;
}

internal class TestSscmFolders : ISscmFolders
{
    public string DataDir => ".";
}

internal static class PathExtensions
{
    public static string EnsureDirectory(this string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        return filePath;
    }
}
