using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests.Mocks;

namespace SSCM.StarCitizen.Tests;

public class SCFolders_Tests
{
    [Test]
    public void GameConfigDir_UsesGameRootAndUppercaseConfiguredEnvironment()
    {
        // Arrange
        var gameRoot = Path.Combine(Path.GetTempPath(), TestContext.CurrentContext.Test.ID, "StarCitizen");
        var expected = Path.Combine(gameRoot, "PTU", SCFolders.SC_PROFILES_DEFAULT_DIR);
        Directory.CreateDirectory(expected);

        var folders = CreateFolders(gameRoot, "ptu");

        // Act
        var actual = folders.GameConfigDir;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GameConfigDir_UsesCurrentEnvironment()
    {
        // Arrange
        var gameRoot = Path.Combine(Path.GetTempPath(), TestContext.CurrentContext.Test.ID, "StarCitizen");
        var expected = Path.Combine(gameRoot, "HOTFIX", SCFolders.SC_PROFILES_DEFAULT_DIR);
        Directory.CreateDirectory(expected);
        var folders = CreateFolders(gameRoot, "live");

        // Act
        folders.Environment = "hotfix";
        var actual = folders.GameConfigDir;

        // Assert
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GameConfigDir_UsesLiveWhenEnvironmentIsNotConfigured()
    {
        // Arrange
        var gameRoot = Path.Combine(Path.GetTempPath(), TestContext.CurrentContext.Test.ID, "StarCitizen");
        var expected = Path.Combine(gameRoot, "LIVE", SCFolders.SC_PROFILES_DEFAULT_DIR);
        Directory.CreateDirectory(expected);

        var folders = CreateFolders(gameRoot, environment: null);

        // Act
        var actual = folders.GameConfigDir;

        // Assert
        Assert.AreEqual("LIVE", folders.Environment);
        Assert.AreEqual(expected, actual);
    }

    [Test]
    public void GameConfigDir_ThrowsWhenConstructedPathDoesNotExist()
    {
        // Arrange
        var gameRoot = Path.Combine(Path.GetTempPath(), TestContext.CurrentContext.Test.ID, "StarCitizen");
        var folders = CreateFolders(gameRoot, "hotfix");
        var expected = Path.Combine(gameRoot, "HOTFIX", SCFolders.SC_PROFILES_DEFAULT_DIR);

        // Act
        var ex = Assert.Throws<DirectoryNotFoundException>(() => _ = folders.GameConfigDir);

        // Assert
        Assert.That(ex!.Message, Does.Contain(expected));
    }

    private static SCFolders CreateFolders(string gameRoot, string? environment)
    {
        var configValues = new Dictionary<string, string?>
        {
            [$"{nameof(SCFolders)}:{nameof(SCFolders.GameRoot)}"] = gameRoot,
            [$"{nameof(SCFolders)}:{nameof(SCFolders.ScDataDir)}"] = ".",
        };
        if (environment != null) configValues[$"{nameof(SCFolders)}:{nameof(SCFolders.Environment)}"] = environment;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();
        var sscmFolders = new TestSscmFolders();

        return new SCFolders(new PlatformForTest(), sscmFolders, config);
    }

    private class TestSscmFolders : ISscmFolders
    {
        public string DataDir => ".";
    }
}
