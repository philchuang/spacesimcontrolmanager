using NUnit.Framework;
using SSCM.Core;
using SSCM.StarCitizen;
using SSCM.StarCitizen.Tests.Mocks;
using SSCM.Tests;
using SSCM.Tests.Mocks;

namespace SSCM.StarCitizen.Tests;

[TestFixture]
public class MappingImporter_Read_Tests
{
    private readonly MappingImporter _importer;
    private readonly IPlatform _platform;

    private SCMappingData? _data;

    public MappingImporter_Read_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._importer = new MappingImporter(this._platform, Samples.GetActionMapsXmlPath());
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

        // silly unreachable code to get rid of warnings
        if (this._data == null) return;

        Assert.AreEqual(this._platform.UtcNow, this._data.ReadTime);
        Assert.AreEqual(4, this._data.Inputs.Count);
        Assert.AreEqual(115, this._data.Mappings.Count);
        Assert.AreEqual(98, this._data.Mappings.Count(m => m.Preserve));
        Assert.AreEqual(17, this._data.Mappings.Count(m => !m.Preserve));
    }

    [Test]
    public void Read_LoadsInputs()
    {
        Assert.NotNull(this._data);

        // silly unreachable code to get rid of warnings
        if (this._data == null) return;

        var expected = new SCInputDevice[] {
            new SCInputDevice { Type = "keyboard", Instance = 1, Preserve = true, Product = "Keyboard  {6F1D2B61-D5A0-11CF-BFC7-444553540000}" },
            new SCInputDevice { Type = "gamepad", Instance = 1, Preserve = true, Product = "Controller (Gamepad)", Settings = new SCInputDeviceSetting[] {
                new SCInputDeviceSetting { Name = "flight_view", Parent = "gamepad-1-Controller (Gamepad)", Preserve = true, Properties = new Dictionary<string, string> { { "exponent", "1" } } }
            } },
            new SCInputDevice { Type = "joystick", Instance = 1, Preserve = true, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" , Settings = new SCInputDeviceSetting[] {
                new SCInputDeviceSetting { Name = "flight_move_pitch", Parent = "joystick-1- VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}", Preserve = true, Properties = new Dictionary<string, string> { { "nonlinearity_curve", "<nonlinearity_curve><point in=\"0\" out=\"0\" /><point in=\"0.1\" out=\"0.063095726\" /><point in=\"0.2\" out=\"0.14495592\" /><point in=\"0.30000001\" out=\"0.23580092\" /><point in=\"0.40000001\" out=\"0.33302128\" /><point in=\"0.44116619\" out=\"0.56157923\" /><point in=\"0.60000002\" out=\"0.54172826\" /><point in=\"0.69999999\" out=\"0.65180492\" /><point in=\"0.80000001\" out=\"0.765082\" /><point in=\"0.90000004\" out=\"0.88123357\" /><point in=\"1\" out=\"1\" /></nonlinearity_curve>" } } },
            } },
            new SCInputDevice { Type = "joystick", Instance = 2, Preserve = true, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Settings = new SCInputDeviceSetting[] {
                new SCInputDeviceSetting { Name = "flight_move_strafe_vertical", Parent = "joystick-2- VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Preserve = true, Properties = new Dictionary<string, string> { { "invert", "1" } } },
                new SCInputDeviceSetting { Name = "flight_move_strafe_longitudinal", Parent = "joystick-2- VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Preserve = true, Properties = new Dictionary<string, string> { { "invert", "1" } } },
            } },
        };
        Assert2.EnumerableEquals(expected, _data.Inputs, AssertSscm.AreEqual);
    }

    [Test]
    public void Read_LoadsMappings()
    {
        Assert.NotNull(this._data);

        // silly unreachable code to get rid of warnings
        if (this._data == null) return;

        // only do a partial comparison
        var mappings = new SCMapping[] {
            new SCMapping { ActionMap = "seat_general", Action = "v_toggle_mining_mode", Input = "js2_button56", MultiTap = null, InputType = "joystick", Preserve = true },
            new SCMapping { ActionMap = "seat_general", Action = "v_toggle_quantum_mode", Input = "js2_button19", MultiTap = null, InputType = "joystick", Preserve = true },
            new SCMapping { ActionMap = "spaceship_targeting", Action = "v_target_unlock_selected", Input = "js1_button16", MultiTap = 2, InputType = "joystick", Preserve = true },
            new SCMapping { ActionMap = "spaceship_view", Action = "v_view_pitch", Input = "js1_ ", MultiTap = null, InputType = "joystick", Preserve = false },
        };

        var expected = mappings.ToDictionary(m => $"{m.ActionMap}-{m.Action}-{m.InputType}");
        var actual = this._data.Mappings.ToDictionary(m => $"{m.ActionMap}-{m.Action}-{m.InputType}");

        Assert2.DictionaryEquals(expected, actual, true, AssertSscm.AreEqual);
    }
}
