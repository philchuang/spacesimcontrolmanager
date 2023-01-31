using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using SSCM.Elite.Tests.Mocks;
using SSCM.Tests;
using SSCM.Tests.Mocks;

namespace SSCM.Elite.Tests;

#pragma warning disable CS8602

[TestFixture]
public class MappingImporter_Read_Tests
{
    private readonly MappingImporter _importer;
    private readonly IPlatform _platform;

    private EDMappingData? _data;

    public MappingImporter_Read_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._importer = new MappingImporter(this._platform, Path.Combine(Directory.GetCurrentDirectory(), "Data", "custom40binds_0.xml"));
    }

    [OneTimeSetUp]
    public async Task Init()
    {
        this._data = await _importer.Read();
        this._importer.StandardOutput += s => TestContext.Out.WriteLine($"[STD  ] {s}");
        this._importer.WarningOutput  += s => TestContext.Out.WriteLine($"[WARN ] {s}");
        this._importer.DebugOutput    += s => TestContext.Out.WriteLine($"[DEBUG] {s}");
    }

    [Test]
    public void Read_HasCorrectDataCounts()
    {
        Assert.NotNull(this._data);

        Assert.AreEqual(this._platform.UtcNow, this._data.ReadTime);
        Assert.AreEqual(87, this._data.Settings.Count);
        Assert.AreEqual(396, this._data.Mappings.Count);
        Assert.AreEqual(346, this._data.Mappings.SelectMany(m => new[] { m.Binding, m.Primary, m.Secondary }).Count(b => b?.Preserve == true));
        Assert.AreEqual(384, this._data.Mappings.SelectMany(m => new[] { m.Binding, m.Primary, m.Secondary }).Count(b => b?.Preserve == false));
        Assert.AreEqual(29, this._data.Mappings.SelectMany(m => m.Settings).Count(s => s.Preserve));
        Assert.AreEqual(120, this._data.Mappings.SelectMany(m => m.Settings).Count(s => !s.Preserve));
        Assert.AreEqual(72, this._data.Settings.Count(s => s.Preserve));
        Assert.AreEqual(15, this._data.Settings.Count(s => !s.Preserve));
    }

    [Test]
    public void Read_LoadsMappings()
    {
        Assert.NotNull(this._data);

        // only do a partial comparison
        var mappings = EDDataSerializer_Write_Tests.CreateTestData().Mappings;

        var expected = mappings.ToDictionary(m => m.Id);
        var actual = this._data.Mappings.ToDictionary(m => m.Id);

        Assert2.DictionaryEquals(expected, actual, true, AssertED.AreEqual);
    }

    [Test]
    public void Read_LoadsSettings()
    {
        Assert.NotNull(this._data);

        // only do a partial comparison
        var settings = new EDMappingSetting[] {
            new EDMappingSetting { Group = "General-FreeCamera", Name = "FreeCamMouseSensitivity", Value = "5.00000000", Preserve = true },
            new EDMappingSetting { Group = "Ship-HeadlookMode", Name = "HeadlookResetOnToggle", Value = "1", Preserve = true },
            new EDMappingSetting { Group = "Ship-Miscellaneous", Name = "MuteButtonMode", Value = "mute_toggle", Preserve = true },
            new EDMappingSetting { Group = "TBD", Name = "EnableRumbleTrigger", Value = "1", Preserve = false },
        };

        var expected = settings.ToDictionary(s => s.Id);
        var actual = this._data.Settings.ToDictionary(s => s.Id);

        Assert2.DictionaryEquals(expected, actual, true, AssertED.AreEqual);
    }
}
