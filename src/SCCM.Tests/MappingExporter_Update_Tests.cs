using NUnit.Framework;
using SCCM.Core;
using SCCM.Tests.Mocks;
using System.Xml.Linq;
using static SCCM.Tests.Extensions;

namespace SCCM.Tests;

[TestFixture]
public class MappingExporter_Update_Tests
{
    private readonly MappingExporter _updater;
    private readonly IPlatform _platform;
    private readonly IFolders _folders;
    private MappingData _data = new MappingData();
    private XDocument? _result = null;
    private XElement? _actionMapsElement = null;
    private XElement? _actionProfilesDefaultElement = null;

    private string GetTestXmlPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "actionmaps.xml")).FullName;
    }

    private async Task<XDocument> LoadTestXml()
    {
        using (var fs = new FileStream(this.GetTestXmlPath(), FileMode.Open))
        {
            var ct = new CancellationToken();
            return await XDocument.LoadAsync(fs, LoadOptions.None, ct);
        }
    }


    public MappingExporter_Update_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._folders = new FoldersForTest();
        System.IO.File.Copy(Samples.GetActionMapsXmlPath(), this.GetTestXmlPath(), true);
        this._updater = new MappingExporter(this._platform, this._folders, this.GetTestXmlPath());
    }

    private async Task Act()
    {
        await this._updater.Update(this._data);
        this._result = await this.LoadTestXml();
        if (this._result != null)
        {
            if (this._result.Root == null)
            {
                throw new InvalidDataException($"Expecting <ActionMaps>, found nothing!");
            }

            if (!this._result.Root.Name.LocalName.Equals("ActionMaps"))
            {
                throw new InvalidDataException($"Expecting <ActionMaps>, found <{this._result.Root.Name.LocalName}>!");
            }

            this._actionMapsElement = this._result.Root;
            this._actionProfilesDefaultElement = this._actionMapsElement.GetChildren("ActionProfiles").Single(ap => ap.GetAttribute("profileName") == "default");
        }
    }

    private void Arrange_Default_MappingData()
    {
        this._data = new MappingData {
            Inputs = {
                new InputDevice { Type = "keyboard", Instance = 1, Preserve = true, Product = "Keyboard  {6F1D2B61-D5A0-11CF-BFC7-444553540000}" },
                new InputDevice { Type = "gamepad", Instance = 1, Preserve = true, Product = "Controller (Gamepad)", Settings = new InputDeviceSetting[] {
                    new InputDeviceSetting { Name = "flight_view", Preserve = true, Properties = new Dictionary<string, string> { { "exponent", "1" } } }
                } },
                new InputDevice { Type = "joystick", Instance = 1, Preserve = true, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" , Settings = new InputDeviceSetting[] {
                    new InputDeviceSetting { Name = "flight_move_pitch", Preserve = true, Properties = new Dictionary<string, string>() },
                } },
                new InputDevice { Type = "joystick", Instance = 2, Preserve = true, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Settings = new InputDeviceSetting[] {
                    new InputDeviceSetting { Name = "flight_move_strafe_vertical", Preserve = true, Properties = new Dictionary<string, string> { { "invert", "1" } } },
                    new InputDeviceSetting { Name = "flight_move_strafe_longitudinal", Preserve = true, Properties = new Dictionary<string, string> { { "invert", "1" } } },
                } },
            },
            Mappings = {
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_mining_mode", Input = "js2_button56" },
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_quantum_mode", Input = "js2_button19" },
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_scan_mode", Input = "js2_button56" },
                new Mapping { ActionMap = "spaceship_general", Action = "v_close_all_doors", Input = "js2_button49" },
                new Mapping { ActionMap = "spaceship_general", Action = "v_flightready", Input = "js2_button52" },
                new Mapping { ActionMap = "spaceship_general", Action = "v_lock_all_doors", Input = "js2_button46" },
                new Mapping { ActionMap = "spaceship_general", Action = "v_open_all_doors", Input = "js2_button51" },
                new Mapping { ActionMap = "spaceship_general", Action = "v_toggle_all_doorlocks", Input = "js2_button47" },
                new Mapping { ActionMap = "spaceship_general", Action = "v_toggle_all_doors", Input = "js2_button50" },
                new Mapping { ActionMap = "spaceship_general", Action = "v_unlock_all_doors", Input = "js2_button48" },
                new Mapping { ActionMap = "spaceship_view", Action = "v_view_cycle_fwd", Input = "js2_button1" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_afterburner", Input = "js2_button3" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_atc_request", Input = "js2_button8" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_autoland", Input = "js2_button10" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_ifcs_speed_limiter_reset_scm", Input = "js2_hat1_right" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_ifcs_toggle_cruise_control", Input = "js2_hat1_left" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_ifcs_toggle_vector_decoupling", Input = "js2_button4" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_roll", Input = "js1_x" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_space_brake", Input = "js2_button5" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_speed_range_down", Input = "js2_hat1_down" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_speed_range_up", Input = "js2_hat1_up" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_strafe_lateral", Input = "js2_x" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_strafe_longitudinal", Input = "js2_y" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_strafe_vertical", Input = "js2_rotz" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_toggle_landing_system", Input = "js2_button7" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_toggle_relative_mouse_mode", Input = "kb1_slash" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_toggle_vtol", Input = "js2_button9" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_transform_deploy", Input = "js2_button61" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_transform_retract", Input = "js2_button58" },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_yaw", Input = "js1_rotz" },
            }
        };
    }

    private void AssertBasics()
    {
        Assert.NotNull(this._result);
        Assert.NotNull(this._actionMapsElement);
        Assert.NotNull(this._actionProfilesDefaultElement);
    }

    private XElement GetActionElement(string actionmapName, string actionName)
    {
        // silly code to prevent warnings
        if (this._result == null || this._actionMapsElement == null || this._actionProfilesDefaultElement == null) throw new Exception();

        return this._actionProfilesDefaultElement
            .GetChildren("actionmap").Single(actionmap => actionmap.GetAttribute("name") == actionmapName)
            .GetChildren("action").Single(action => action.GetAttribute("name") == actionName);
    }

    [Test]
    public async Task Update_removes_inputs()
    {
        // NOTE I did this wrong, won't ever happen - rewrite into another test

        // Arrange
        this.Arrange_Default_MappingData();
        // remove joystick 2
        var removedInput = this._data.Inputs[3];
        this._data.Inputs.Remove(removedInput);
        this._data.GetRelatedMappings(removedInput).ToList().ForEach(m => this._data.Mappings.Remove(m));

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._result == null || this._actionMapsElement == null || this._actionProfilesDefaultElement == null) throw new Exception();

        // match remaining inputs
        var inputElements = this._actionProfilesDefaultElement.GetChildren("options").ToList();
        Assert.AreEqual(3, inputElements.Where((option, idx) => {
            var input = this._data.Inputs[idx];
            return option.GetAttribute("type") == input.Type &&
                option.GetAttribute("instance") == input.Instance.ToString() &&
                option.GetAttribute("product") == input.Product;
        }));
        // assert no related mappings
        Assert.IsFalse(
            this._actionProfilesDefaultElement.GetChildren("actionmaps")
                .SelectMany(amElement => amElement.GetChildren("action"))
                .SelectMany(aElement => aElement.GetChildren("rebind"))
                .Select(rebindElement => rebindElement.GetAttribute("input"))
                .Where(input => input != null && input.StartsWith(removedInput.GetMappingPrefix()))
                .Any());
    }

    [Test]
    public async Task Update_overwrites_mapping()
    {
        // Arrange
        this.Arrange_Default_MappingData();
        var changedMapping = this._data.Mappings.Single(m => m.ActionMap == "spaceship_movement" && m.Action == "v_ifcs_toggle_cruise_control");
        var originalInput = changedMapping.Input;
        var changedInput = changedMapping.Input = RandomString();

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._result == null || this._actionMapsElement == null || this._actionProfilesDefaultElement == null) return;

        var changedActionElement = this.GetActionElement(changedMapping.ActionMap, changedMapping.Action);
        Assert.NotNull(changedActionElement, nameof(changedActionElement));
        Assert.AreEqual(changedInput, changedActionElement.GetChildren("rebind").Single().GetAttribute("input"));
    }

    [Test]
    public async Task Update_ignores_mapping()
    {
        Assert.Fail();
    }

    [Test]
    public async Task Update_overwrites_input_setting()
    {
        Assert.Fail();
    }

    [Test]
    public async Task Update_ignores_input_setting()
    {
        Assert.Fail();
    }

    [Test]
    public async Task Update_overwrites_inputs()
    {
        Assert.Fail();
    }
}