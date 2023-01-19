using NUnit.Framework;
using SCCM.Core;

namespace SCCM.Tests;

[TestFixture]
public class Reader_Read_Tests
{
    private readonly Reader _reader;

    private static string GetSamplesDir()
    {
        var working = System.IO.Directory.GetCurrentDirectory();
        return new System.IO.DirectoryInfo(System.IO.Path.Combine(working, "../../../../../samples")).FullName;
    }

    private static string GetSampleXmlPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(GetSamplesDir(), "actionmaps.3.17.4.xml")).FullName;
    }

    public Reader_Read_Tests()
    {
        _reader = new Reader(GetSampleXmlPath());
    }

    [OneTimeSetUp]
    public async Task Init()
    {

        await _reader.Read();
    }

    [Test]
    public void Read_LoadsInputs()
    {
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
        Assert2.ListEquals(
            expected,
            (IList<InputDevice>) _reader.Inputs,
            (e, a) => {
                Assert.AreEqual(e.Type, a.Type);
                Assert.AreEqual(e.Instance, a.Instance);
                Assert.AreEqual(e.Product, a.Product);
                Assert2.ListEquals(e.Settings, a.Settings, (e2, a2) => {
                    Assert2.DictionaryEquals(e2.Properties, a2.Properties);
                });
            }
        );
    }

    [Test]
    public void Read_LoadsMappings()
    {
        var expected = new Mapping[] {
            new Mapping { ActionMap = "seat_general", Action = "v_toggle_mining_mode", Input = "js2_button56" },
        };

        Assert2.ListEquals(
            expected,
            (IList<Mapping>) _reader.Mappings,
            (e, a) => {
                // TODO
            }
        );
    }
}
