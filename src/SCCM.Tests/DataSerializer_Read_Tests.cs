using NUnit.Framework;
using SCCM.Core;
using static SCCM.Tests.Extensions;

namespace SCCM.Tests;

[TestFixture]
public class DataSerializer_Read_Tests
{
    private DataSerializer _serializer = new DataSerializer(string.Empty);

    private string TestDataPath => new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TestContext.CurrentContext.Test.Name, "scmappings.json")).FullName;

    public DataSerializer_Read_Tests()
    {
    }

    [SetUp]
    public void Init()
    {
        System.IO.Directory.CreateDirectory(new FileInfo(this.TestDataPath).DirectoryName);
        System.IO.File.Copy(Samples.GetPartialMappingsJsonPath(), this.TestDataPath, true);
        this._serializer = new DataSerializer(this.TestDataPath);
    }

    [TearDown]
    protected void Cleanup()
    {
        System.IO.Directory.Delete(new FileInfo(this.TestDataPath).DirectoryName, true);
    }

    [Test]
    public async Task Read_MatchesSampleData()
    {
        // matches data from mappings.3.17.4.sample.json
        var expected = new MappingData
        {
            ReadTime = DateTime.Parse("2022-12-22T05:42:36.1532351Z").ToUniversalTime(),
            Inputs = new InputDevice[] {
                new InputDevice { Type = "keyboard", Instance = 1, Product = "Keyboard  {6F1D2B61-D5A0-11CF-BFC7-444553540000}" },
                new InputDevice { Type = "gamepad", Instance = 1, Product = "Controller (Gamepad)", Settings = new InputDeviceSetting[] {
                    new InputDeviceSetting { Name = "flight_view", Properties = new Dictionary<string, string> { { "exponent", "1" } } }
                } },
                new InputDevice { Type = "joystick", Instance = 1, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" },
                new InputDevice { Type = "joystick", Instance = 2, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Settings = new InputDeviceSetting[] {
                    new InputDeviceSetting { Name = "flight_move_strafe_vertical", Properties = new Dictionary<string, string> { { "invert", "1" } } },
                    new InputDeviceSetting { Name = "flight_move_strafe_longitudinal", Properties = new Dictionary<string, string> { { "invert", "1" } } },
                } },
            },
            Mappings = new Mapping[] {
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_mining_mode", Input = "js2_button56", InputType = "joystick", MultiTap = null, Preserve = true },
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_quantum_mode", Input = "js2_button19", InputType = "joystick", MultiTap = null, Preserve = true },
                new Mapping { ActionMap = "spaceship_view", Action = "v_view_pitch", Input = "js1_ ", InputType = "joystick", MultiTap = null, Preserve = false },
                new Mapping { ActionMap = "spaceship_targeting", Action = "v_target_unlock_selected", Input = "js1_button16", InputType = "joystick", MultiTap = 2, Preserve = true },
            },
        };
        var actual = await this._serializer.Read();

        AssertSccm.AreEqual(expected, actual);
    }

    [Test]
    public async Task Handles_Malformed_MappingsFile()
    {
        // Arrange
        await System.IO.File.WriteAllTextAsync(this.TestDataPath, RandomString());

        // Act
        var ex = Assert.ThrowsAsync<SccmException>(() => this._serializer.Read());
        Assert.IsTrue(ex.Message.StartsWith("Could not read SCCM mapping data file at"));
    }

    [Test]
    public async Task Handles_NotFound_MappingsFile()
    {
        // Arrange
        System.IO.File.Delete(this.TestDataPath);

        // Act
        var result = await this._serializer.Read();

        // Assert
        Assert.IsNull(result);
    }
}
