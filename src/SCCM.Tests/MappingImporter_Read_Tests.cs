using NUnit.Framework;
using SCCM.Core;

namespace SCCM.Tests;

[TestFixture]
public class MappingImporter_Read_Tests
{
    private readonly MappingImporter _importer;
    private MappingData? _data;

    private static string GetSamplesDir()
    {
        var working = System.IO.Directory.GetCurrentDirectory();
        return new System.IO.DirectoryInfo(System.IO.Path.Combine(working, "../../../../../samples")).FullName;
    }

    private static string GetSampleXmlPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(GetSamplesDir(), "actionmaps.3.17.4.xml")).FullName;
    }

    public MappingImporter_Read_Tests()
    {
        _importer = new MappingImporter(GetSampleXmlPath());
        // TODO figure out how to make this show
        _importer.StandardOutput += s => System.Diagnostics.Debug.WriteLine($"[STD  ] {s}");
        _importer.WarningOutput  += s => System.Diagnostics.Debug.WriteLine($"[WARN ] {s}");
        _importer.DebugOutput    += s => System.Diagnostics.Debug.WriteLine($"[DEBUG] {s}");
    }

    [OneTimeSetUp]
    public async Task Init()
    {
        this._data = await _importer.Read();
    }

    [Test]
    public void Read_LoadsInputs()
    {
        Assert.NotNull(this._data);

        var expected = new InputDevice[] {
            new InputDevice { Type = "keyboard", Instance = 1, Product = "Keyboard  {6F1D2B61-D5A0-11CF-BFC7-444553540000}" },
            new InputDevice { Type = "gamepad", Instance = 1, Product = "Controller (Gamepad)", Settings = new InputDeviceSetting[] {
                new InputDeviceSetting { Name = "flight_view", Properties = new Dictionary<string, string> { { "exponent", "1" } } }
            } },
            new InputDevice { Type = "joystick", Instance = 1, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" },
            new InputDevice { Type = "joystick", Instance = 2, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Settings = new InputDeviceSetting[] {
                new InputDeviceSetting { Name = "flight_move_strafe_vertical", Properties = new Dictionary<string, string> { { "invert", "1" } } },
                new InputDeviceSetting { Name = "flight_move_strafe_longitudinal", Properties = new Dictionary<string, string> { { "invert", "1" } } },
            } },
        };
        Assert2.EnumerableEquals(
            expected,
            _data.Inputs,
            (e, a) => {
                Assert.AreEqual(e.Type, a.Type);
                Assert.AreEqual(e.Instance, a.Instance);
                Assert.AreEqual(e.Product, a.Product);
                Assert2.EnumerableEquals(e.Settings, a.Settings, (e2, a2) => {
                    Assert2.DictionaryEquals(e2.Properties, a2.Properties);
                });
            }
        );
    }

    [Test]
    public void Read_LoadsMappings()
    {
        // only do a partial comparison
        var mappings = new Mapping[] {
            new Mapping { ActionMap = "seat_general", Action = "v_toggle_mining_mode", Input = "js2_button56", MultiTap = null, Preserve = true },
            new Mapping { ActionMap = "seat_general", Action = "v_toggle_quantum_mode", Input = "js2_button19", MultiTap = null, Preserve = true },
            new Mapping { ActionMap = "spaceship_targeting", Action = "v_target_unlock_selected", Input = "js1_button16", MultiTap = 2, Preserve = true },
        };

        var expected = mappings.ToDictionary(m => $"{m.ActionMap}-{m.Action}");
        var actual = _data.Mappings.ToDictionary(m => $"{m.ActionMap}-{m.Action}");

        Assert2.DictionaryEquals(
            expected,
            actual,
            true,
            (e, a) => {
                Assert.AreEqual(e.ActionMap, a.ActionMap);
                Assert.AreEqual(e.Action, a.Action);
                Assert.AreEqual(e.Input, a.Input);
                Assert.AreEqual(e.MultiTap, a.MultiTap);
                Assert.AreEqual(e.Preserve, a.Preserve);
            }
        );
    }
}
