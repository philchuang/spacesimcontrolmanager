using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using SSCM.Tests.Mocks;

namespace SSCM.Elite.Tests;

public class EDFolders_Tests
{
    private string _root = string.Empty;

    [SetUp]
    public void SetUp()
    {
        this._root = Path.Combine(Path.GetTempPath(), "sscm-tests", TestContext.CurrentContext.Test.ID);
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
        Directory.CreateDirectory(this._root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this._root)) Directory.Delete(this._root, recursive: true);
    }

    [Test]
    public void GameConfigPath_UsesTheHighestCustomBindsFileInTheConfiguredDirectory()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "Custom.3.0.binds", "Custom.4.0.binds", "Custom.4.1.binds", "Custom.4.2.binds");

        // Act
        var folders = this.CreateFolders(gameDir);

        // Assert
        Assert.AreEqual(gameDir, folders.GameConfigDir);
        Assert.AreEqual(Path.Combine(gameDir, "Custom.4.2.binds"), folders.GameConfigPath);
    }

    [Test]
    public void GameConfigPath_UsesCustom30WhenItIsTheOnlyFile()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "Custom.3.0.binds");

        // Act
        var folders = this.CreateFolders(gameDir);

        // Assert
        Assert.AreEqual(Path.Combine(gameDir, "Custom.3.0.binds"), folders.GameConfigPath);
    }

    [Test]
    public void GameConfigPath_ThrowsWhenTheConfiguredDirectoryHasNoCustomBindsFile()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "StartPreset.binds");

        // Act
        var ex = Assert.Throws<FileNotFoundException>(() => this.CreateFolders(gameDir));

        // Assert
        Assert.That(ex!.Message, Does.Contain("Custom.{major}.{minor}.binds").And.Contain(gameDir));
    }

    [Test]
    public void EliteDataDir_UsesTheConfiguredValue()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "Custom.4.2.binds");
        var dataDir = this.MakeDir("data");

        // Act
        var folders = this.CreateFolders(gameDir, dataDir);

        // Assert
        Assert.AreEqual(dataDir, folders.EliteDataDir);
    }

    [Test]
    public void GameRoot_DefaultsToTheSteamOdysseyProduct()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "Custom.4.2.binds");

        // Act
        var folders = this.CreateFolders(gameDir);

        // Assert
        Assert.AreEqual(EDFolders.DefaultGameRoot(), folders.GameRoot);
        Assert.That(folders.GameRoot, Does.EndWith(Path.Combine("Steam", "steamapps", "common", "Elite Dangerous", "Products", "elite-dangerous-odyssey-64")));
        Assert.AreEqual(EDFolders.DEFAULT_BINDS, folders.DefaultBinds);
        Assert.AreEqual(Path.Combine(folders.GameRoot, "ControlSchemes", "GenericJoystick.binds"), folders.DefaultBindsPath);
    }

    [Test]
    public void GameRoot_UsesTheConfiguredValue()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "Custom.4.2.binds");
        var install = this.MakeDir("install");

        // Act
        var folders = this.CreateFolders(gameDir, gameRoot: install);

        // Assert
        Assert.AreEqual(install, folders.GameRoot);
        Assert.AreEqual(Path.Combine(install, "ControlSchemes", "GenericJoystick.binds"), folders.DefaultBindsPath);
    }

    [Test]
    public void DefaultBinds_UsesTheConfiguredSchemeName()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "Custom.4.2.binds");
        var install = this.MakeDir("install");

        // Act
        var folders = this.CreateFolders(gameDir, gameRoot: install, defaultBinds: "KeyboardMouseOnly");

        // Assert
        Assert.AreEqual("KeyboardMouseOnly", folders.DefaultBinds);
        Assert.AreEqual(Path.Combine(install, "ControlSchemes", "KeyboardMouseOnly.binds"), folders.DefaultBindsPath);
    }

    [Test]
    public void DefaultBinds_StripsTheBindsExtensionWhenConfigured()
    {
        // Arrange
        var gameDir = this.MakeDir("game", "Custom.4.2.binds");

        // Act
        var folders = this.CreateFolders(gameDir, defaultBinds: "Empty.binds");

        // Assert
        Assert.AreEqual("Empty", folders.DefaultBinds);
        Assert.AreEqual(Path.Combine(folders.GameRoot, "ControlSchemes", "Empty.binds"), folders.DefaultBindsPath);
    }

    private EDFolders CreateFolders(string gameConfigDir, string? eliteDataDir = null, string? gameRoot = null, string? defaultBinds = null)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{nameof(EDFolders)}:{nameof(EDFolders.GameConfigDir)}"] = gameConfigDir,
            [$"{nameof(EDFolders)}:{nameof(EDFolders.EliteDataDir)}"] = eliteDataDir ?? this.MakeDir("data"),
        };
        if (gameRoot != null) values[$"{nameof(EDFolders)}:{nameof(EDFolders.GameRoot)}"] = gameRoot;
        if (defaultBinds != null) values[$"{nameof(EDFolders)}:{nameof(EDFolders.DefaultBinds)}"] = defaultBinds;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new EDFolders(new PlatformForTest(), new TestSscmFolders(), config);
    }

    private string MakeDir(string name, params string[] fileNames)
    {
        var dir = Path.Combine(this._root, name);
        Directory.CreateDirectory(dir);
        foreach (var fileName in fileNames) File.WriteAllText(Path.Combine(dir, fileName), "x");
        return dir;
    }

    private class TestSscmFolders : ISscmFolders
    {
        public string DataDir => ".";
    }
}
