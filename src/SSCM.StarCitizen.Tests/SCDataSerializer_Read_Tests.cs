using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using static SSCM.Tests.Extensions;

namespace SSCM.StarCitizen.Tests;

[TestFixture]
public class SCDataSerializer_Read_Tests : DataSerializer_Read_Tests<SCMappingData>
{
    protected override string SourceFilePath => Samples.GetPartialMappingsJsonPath();

    public SCDataSerializer_Read_Tests()
    {
    }

    protected override SCMappingData CreateDataForRead()
    {
        // matches data from mappings.3.17.4.sample.json
        var expected = new SCMappingData
        {
            ReadTime = DateTime.Parse("2022-12-22T05:42:36.1532351Z").ToUniversalTime(),
            Inputs = new SCInputDevice[] {
                new SCInputDevice { Type = "keyboard", Instance = 1, Product = "Keyboard  {6F1D2B61-D5A0-11CF-BFC7-444553540000}" },
                new SCInputDevice { Type = "gamepad", Instance = 1, Product = "Controller (Gamepad)", Settings = new SCInputDeviceSetting[] {
                    new SCInputDeviceSetting { Name = "flight_view", Properties = new Dictionary<string, string> { { "exponent", "1" } } }
                } },
                new SCInputDevice { Type = "joystick", Instance = 1, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" },
                new SCInputDevice { Type = "joystick", Instance = 2, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Settings = new SCInputDeviceSetting[] {
                    new SCInputDeviceSetting { Name = "flight_move_strafe_vertical", Properties = new Dictionary<string, string> { { "invert", "1" } } },
                    new SCInputDeviceSetting { Name = "flight_move_strafe_longitudinal", Properties = new Dictionary<string, string> { { "invert", "1" } } },
                } },
            },
            Mappings = new SCMapping[] {
                new SCMapping { ActionMap = "seat_general", Action = "v_toggle_mining_mode", Input = "js2_button56", InputType = "joystick", MultiTap = null, Preserve = true },
                new SCMapping { ActionMap = "seat_general", Action = "v_toggle_quantum_mode", Input = "js2_button19", InputType = "joystick", MultiTap = null, Preserve = true },
                new SCMapping { ActionMap = "spaceship_view", Action = "v_view_pitch", Input = "js1_ ", InputType = "joystick", MultiTap = null, Preserve = false },
                new SCMapping { ActionMap = "spaceship_targeting", Action = "v_target_unlock_selected", Input = "js1_button16", InputType = "joystick", MultiTap = 2, Preserve = true },
            },
        };
        return expected;
    }

    protected override void AssertAreEqual(SCMappingData? expected, SCMappingData? actual) => AssertSC.AreEqual(expected, actual);
}
