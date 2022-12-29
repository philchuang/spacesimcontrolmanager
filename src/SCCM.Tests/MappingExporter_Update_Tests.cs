using NUnit.Framework;
using SCCM.Core;
using SCCM.Core.SC;
using SCCM.Tests.Mocks;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using static SCCM.Core.SC.Extensions;
using static SCCM.Tests.Extensions;

namespace SCCM.Tests;

[TestFixture]
public class MappingExporter_Update_Tests
{
    private MappingExporter? _updater;
    private readonly IPlatform _platform;
    private readonly ISCFolders _folders;
    private MappingData _data = new MappingData();
    private XDocument? _originalXml = null;
    private XDocument? _updatedXml = null;

    public MappingExporter_Update_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._folders = new SCFoldersForTest();
    }

    [SetUp]
    protected async Task Init()
    {
        this._updater = new MappingExporter(this._platform, this._folders, this.GetTestXmlPath());
        this._updater.StandardOutput += s => TestContext.Out.WriteLine($"[STD  ]\t{s}");
        this._updater.DebugOutput += s => TestContext.Out.WriteLine($"[DEBUG]\t{s}");
        this._updater.WarningOutput += s => TestContext.Out.WriteLine($"[WARN ]\t{s}");

        this._originalXml = await this.LoadXml(Samples.GetActionMapsXmlPath());
    }

    private async Task Act()
    {
        System.IO.Directory.CreateDirectory(new FileInfo(this.GetTestXmlPath()).DirectoryName);
        using (var fs = new FileStream(this.GetTestXmlPath(), FileMode.Create))
        using (var xw = XmlWriter.Create(fs, new XmlWriterSettings { Async = true, Indent = true }))
        {
            var ct = new CancellationToken();
            await this._originalXml.WriteToAsync(xw, ct);
        }
        await this._updater.Update(this._data);
        this._updatedXml = await this.LoadXml(this.GetTestXmlPath());
    }

    private void AssertBasics()
    {
        Assert.NotNull(this._updatedXml);
        Assert.NotNull(this._updatedXml.Root);
        Assert.AreEqual("ActionMaps", this._updatedXml.Root.Name.LocalName);
        Assert.NotNull(this._updatedXml.XPathSelectElement("/ActionMaps/ActionProfiles[@profileName='default']"));
    }

    [TearDown]
    protected void Cleanup()
    {
        System.IO.Directory.Delete(new FileInfo(this.GetTestXmlPath()).DirectoryName, true);
    }

    private string GetTestXmlPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TestContext.CurrentContext.Test.Name, "actionmaps.xml")).FullName;
    }

    private async Task<XDocument> LoadXml(string path)
    {
        using (var fs = new FileStream(path, FileMode.Open))
        {
            var ct = new CancellationToken();
            return await XDocument.LoadAsync(fs, LoadOptions.None, ct);
        }
    }

    private XElement GetInputElement(XDocument xd, InputDevice input)
    {
        if (!string.IsNullOrWhiteSpace(input.Product))
            return xd.XPathSelectElements($"/ActionMaps/ActionProfiles[@profileName='default']/options[@type='{input.Type}' and @instance='{input.Instance}' and @Product='{input.Product}']").SingleOrDefault();

        return xd.XPathSelectElements($"/ActionMaps/ActionProfiles[@profileName='default']/options[@type='{input.Type}' and @instance='{input.Instance}']").SingleOrDefault();
    }

    private XElement GetActionRebindElement(XDocument xd, Mapping mapping)
    {
        return xd.XPathSelectElements($"/ActionMaps/ActionProfiles[@profileName='default']/actionmap[@name='{mapping.ActionMap}']/action[@name='{mapping.Action}']/rebind").SingleOrDefault();
    }

    private void Arrange_Default_MappingData()
    {
        this._data = new MappingData {
            Inputs = {
                new InputDevice { Type = "keyboard", Instance = 1, Preserve = true, Product = "Keyboard  {6F1D2B61-D5A0-11CF-BFC7-444553540000}" },
                new InputDevice { Type = "gamepad", Instance = 1, Preserve = true, Product = "Controller (Gamepad)", Settings = {
                    new InputDeviceSetting { Name = "flight_view", Preserve = true, Properties = new Dictionary<string, string> { { "exponent", "1" } } }
                } },
                new InputDevice { Type = "joystick", Instance = 1, Preserve = true, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" , Settings = {
                    new InputDeviceSetting { Name = "flight_move_pitch", Preserve = true, Properties = new Dictionary<string, string>() },
                } },
                new InputDevice { Type = "joystick", Instance = 2, Preserve = true, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Settings = {
                    new InputDeviceSetting { Name = "flight_move_strafe_vertical", Preserve = true, Properties = new Dictionary<string, string> { { "invert", "1" } } },
                    new InputDeviceSetting { Name = "flight_move_strafe_longitudinal", Preserve = true, Properties = new Dictionary<string, string> { { "invert", "1" } } },
                } },
            },
            Mappings = {
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_mining_mode", Input = "js2_button56", Preserve = true },
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_quantum_mode", Input = "js2_button19", Preserve = true },
                new Mapping { ActionMap = "seat_general", Action = "v_toggle_scan_mode", Input = "js2_button54", Preserve = true },
                new Mapping { ActionMap = "spaceship_general", Action = "v_close_all_doors", Input = "js2_button49", Preserve = true },
                new Mapping { ActionMap = "spaceship_general", Action = "v_flightready", Input = "js2_button52", Preserve = true },
                new Mapping { ActionMap = "spaceship_general", Action = "v_lock_all_doors", Input = "js2_button46", Preserve = true },
                new Mapping { ActionMap = "spaceship_general", Action = "v_open_all_doors", Input = "js2_button51", Preserve = true },
                new Mapping { ActionMap = "spaceship_general", Action = "v_toggle_all_doorlocks", Input = "js2_button47", Preserve = true },
                new Mapping { ActionMap = "spaceship_general", Action = "v_toggle_all_doors", Input = "js2_button50", Preserve = true },
                new Mapping { ActionMap = "spaceship_general", Action = "v_unlock_all_doors", Input = "js2_button48", Preserve = true },
                new Mapping { ActionMap = "spaceship_view", Action = "v_view_cycle_fwd", Input = "js2_button1", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_afterburner", Input = "js2_button3", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_atc_request", Input = "js2_button8", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_autoland", Input = "js2_button10", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_ifcs_speed_limiter_reset_scm", Input = "js2_hat1_right", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_ifcs_toggle_cruise_control", Input = "js2_hat1_left", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_ifcs_toggle_vector_decoupling", Input = "js2_button4", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_roll", Input = "js1_x", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_space_brake", Input = "js2_button5", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_speed_range_down", Input = "js2_hat1_down", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_speed_range_up", Input = "js2_hat1_up", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_strafe_lateral", Input = "js2_x", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_strafe_longitudinal", Input = "js2_y", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_strafe_vertical", Input = "js2_rotz", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_toggle_landing_system", Input = "js2_button7", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_toggle_relative_mouse_mode", Input = "kb1_slash", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_toggle_vtol", Input = "js2_button9", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_transform_deploy", Input = "js2_button61", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_transform_retract", Input = "js2_button58", Preserve = true },
                new Mapping { ActionMap = "spaceship_movement", Action = "v_yaw", Input = "js1_rotz", Preserve = true },
            }
        };
    }

    private (string, Mapping) Arrange_Update_overwrites_mapping_change(bool preserve)
    {
        this.Arrange_Default_MappingData();
        this._data.Mappings.ToList().ForEach(m => m.Preserve = false);
        var mapping = this._data.Mappings.Single(m => m.ActionMap == "spaceship_movement" && m.Action == "v_ifcs_toggle_cruise_control");
        mapping.Preserve = preserve;
        var actionRebindElement = this.GetActionRebindElement(this._originalXml, mapping);
        var originalInputValue = actionRebindElement.GetAttribute("input");
        mapping.Input = mapping.Input == originalInputValue ? RandomString() : mapping.Input;
        return (originalInputValue, mapping);
    }
    
    [Test]
    public async Task Update_overwrites_mapping_change()
    {
        // Arrange
        var (originalInputValue, mapping) = this.Arrange_Update_overwrites_mapping_change(true);

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null) return;

        var changedActionRebindElement = this.GetActionRebindElement(this._updatedXml, mapping);
        Assert.NotNull(changedActionRebindElement, nameof(changedActionRebindElement));
        Assert.AreEqual(mapping.Input, changedActionRebindElement.GetAttribute("input"));
    }

    [Test]
    public async Task Update_ignores_mapping_change()
    {
        // Arrange
        var (originalInputValue, mapping) = this.Arrange_Update_overwrites_mapping_change(false);

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null) return;

        var changedActionRebindElement = this.GetActionRebindElement(this._updatedXml, mapping);
        Assert.NotNull(changedActionRebindElement, nameof(changedActionRebindElement));
        Assert.AreEqual(originalInputValue, changedActionRebindElement.GetAttribute("input"));
    }

    [Test]
    public async Task Update_adds_actionmap_and_action()
    {
        // Arrange
        this.Arrange_Default_MappingData();
        this._data.Mappings.ToList().ForEach(m => m.Preserve = false);
        var mapping = new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js2_{RandomString()}", Preserve = true };
        this._data.Mappings.Add(mapping);

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null || this._originalXml == null) return;

        var originalActionRebindElement = this.GetActionRebindElement(this._originalXml, mapping);
        Assert.IsNull(originalActionRebindElement, nameof(originalActionRebindElement));
        var addedActionRebindElement = this.GetActionRebindElement(this._updatedXml, mapping);
        Assert.NotNull(addedActionRebindElement, nameof(addedActionRebindElement));
        Assert.AreEqual(mapping.Input, addedActionRebindElement.GetAttribute("input"));
    }

    [Test]
    public async Task Update_adds_action()
    {
        // Arrange
        this.Arrange_Default_MappingData();
        this._data.Mappings.ToList().ForEach(m => m.Preserve = false);
        var mapping = new Mapping { ActionMap = this._data.Mappings.First().ActionMap, Action = RandomString(), Input = $"js2_{RandomString()}", Preserve = true };
        this._data.Mappings.Add(mapping);

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null || this._originalXml == null) return;

        var originalActionRebindElement = this.GetActionRebindElement(this._originalXml, mapping);
        Assert.IsNull(originalActionRebindElement, nameof(originalActionRebindElement));
        var addedActionRebindElement = this.GetActionRebindElement(this._updatedXml, mapping);
        Assert.NotNull(addedActionRebindElement, nameof(addedActionRebindElement));
        Assert.AreEqual(mapping.Input, addedActionRebindElement.GetAttribute("input"));
    }

    protected (InputDevice, InputDeviceSetting, string, string) Arrange_Update_overwrites_input_setting(bool settingPreserve)
    {
        this.Arrange_Default_MappingData();
        this._data.Inputs.SelectMany(i => i.Settings).ToList().ForEach(s => s.Preserve = false);
        this._data.Mappings.ToList().ForEach(m => m.Preserve = false);
        
        var exportedInput = this._data.Inputs[1];
        var exportedSetting = exportedInput.Settings.First();
        exportedSetting.Preserve = settingPreserve;
        
        var targetInputElement = this.GetInputElement(this._originalXml, exportedInput);
        var targetSettingElement = targetInputElement.Elements().Single();
        var targetSettingValueAttribute = targetSettingElement.Attributes().First();
        var targetSettingValue = targetSettingValueAttribute.Value;

        var exportedSettingValue = exportedSetting.Properties.First();
        exportedSetting.Properties[exportedSettingValue.Key] = exportedSettingValue.Value == targetSettingValue ? RandomString() : targetSettingValue;

        return (exportedInput, exportedSetting, exportedSettingValue.Key, targetSettingValue);
    }

    [Test]
    public async Task Update_overwrites_input_setting_change()
    {
        // Arrange
        var (exportedInput, exportedSetting, exportedSettingValueName, targetSettingValue) = this.Arrange_Update_overwrites_input_setting(settingPreserve: true);

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null) return;

        var changedInputElement = this.GetInputElement(this._updatedXml, exportedInput);
        Assert.NotNull(changedInputElement, nameof(changedInputElement));
        var changedSettingElement = changedInputElement.Elements().Single();
        Assert.NotNull(changedSettingElement, nameof(changedSettingElement));
        var changedSettingValueAttribute = changedSettingElement.Attributes().First();
        var changedSettingValue = changedSettingValueAttribute.Value;
        Assert.AreEqual(exportedSetting.Properties[exportedSettingValueName], changedSettingValue);
    }

    private (InputDevice, InputDeviceSetting) Arrange_XmlInputSetting_MappingData()
    {
        this.Arrange_Default_MappingData();
        // turn off all preserves
        this._data.Inputs.SelectMany(i => i.Settings).ToList().ForEach(s => s.Preserve = false);
        this._data.Mappings.ToList().ForEach(m => m.Preserve = false);
        
        // find first joystick
        var exportedInput = this._data.Inputs.First(i => i.Type == "joystick");
        // add XML setting
        var exportedSetting = new InputDeviceSetting { Name = "flight_move_pitch", Preserve = true, Properties = new Dictionary<string, string> { { "nonlinearity_curve", "<nonlinearity_curve><point in=\"0\" out=\"0\" /><point in=\"0.1\" out=\"0.063095726\" /><point in=\"0.2\" out=\"0.14495592\" /><point in=\"0.30000001\" out=\"0.23580092\" /><point in=\"0.40000001\" out=\"0.33302128\" /><point in=\"0.44116619\" out=\"0.56157923\" /><point in=\"0.60000002\" out=\"0.54172826\" /><point in=\"0.69999999\" out=\"0.65180492\" /><point in=\"0.80000001\" out=\"0.765082\" /><point in=\"0.90000004\" out=\"0.88123357\" /><point in=\"1\" out=\"1\" /></nonlinearity_curve>" } } };
        exportedInput.Settings.Add(exportedSetting);

        return (exportedInput, exportedSetting);
    }

    [Test]
    public async Task Update_overwrites_input_setting_change_xml()
    {
        // Arrange
        var (exportedInput, exportedSetting) = this.Arrange_XmlInputSetting_MappingData();
        var (exportedSettingValueName, exportedSettingValue) = exportedSetting.Properties.Select(kvp => (kvp.Key, kvp.Value)).First();        
        // find related pre-update input node
        var targetInputElement = this.GetInputElement(this._originalXml, exportedInput);
        // find setting node
        var targetSettingElement = targetInputElement.GetChildren(exportedSetting.Name).SingleOrDefault();
        if (targetSettingElement == null)
        {
            // create setting node
            targetSettingElement = new XElement(exportedSetting.Name);
            targetInputElement.Add(targetSettingElement);
        }
        // find setting value node
        var targetSettingValueElement = targetSettingElement.GetChildren(exportedSettingValueName).SingleOrDefault();
        if (targetSettingValueElement == null)
        {
            // create setting value node
            targetSettingValueElement = new XElement(exportedSettingValueName);
            targetSettingElement.Add(targetSettingValueElement);
        }
        // ensure setting value node doesn't match exported setting value
        targetSettingValueElement.RemoveAll();

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null) return;

        var changedInputElement = this.GetInputElement(this._updatedXml, exportedInput);
        Assert.NotNull(changedInputElement, nameof(changedInputElement));
        var changedSettingElement = changedInputElement.GetChildren(exportedSetting.Name).SingleOrDefault();
        Assert.NotNull(changedSettingElement, nameof(changedSettingElement));
        var changedSettingValueElement = changedSettingElement.GetChildren(exportedSettingValueName).SingleOrDefault();
        Assert.NotNull(changedSettingValueElement, nameof(changedSettingValueElement));
        var changedSettingValue = changedSettingValueElement.ToString(SaveOptions.DisableFormatting);
        Assert.AreEqual(exportedSettingValue, changedSettingValue);
    }

    [Test]
    public async Task Update_ignores_input_setting_change()
    {
        // Arrange
        var (exportedInput, exportedSetting, exportedSettingValueName, targetSettingValue) = this.Arrange_Update_overwrites_input_setting(settingPreserve: false);

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null) return;

        var unchangedInputElement = this.GetInputElement(this._updatedXml, exportedInput);
        Assert.NotNull(unchangedInputElement, nameof(unchangedInputElement));
        var unchangedSettingElement = unchangedInputElement.Elements().Single();
        Assert.NotNull(unchangedSettingElement, nameof(unchangedSettingElement));
        var unchangedSettingValueAttribute = unchangedSettingElement.Attributes().First();
        var unchangedSettingValue = unchangedSettingValueAttribute.Value;
        Assert.AreEqual(targetSettingValue, unchangedSettingValue);
    }

    [Test]
    public async Task Update_adds_input_setting()
    {
        // Arrange
        this.Arrange_Default_MappingData();
        this._data.Inputs.SelectMany(i => i.Settings).ToList().ForEach(s => s.Preserve = false);
        this._data.Mappings.ToList().ForEach(m => m.Preserve = false);
        
        var exportedInput = this._data.Inputs[1];
        var exportedSetting = new InputDeviceSetting { Name = "AbcDef", Preserve = true, Properties = { { "GhiJkl", RandomString() } } };
        exportedInput.Settings.Add(exportedSetting);
        
        var targetInputElement = this.GetInputElement(this._originalXml, exportedInput);
        var targetSettingElement = targetInputElement.GetChildren(exportedSetting.Name).SingleOrDefault();

        var exportedSettingValue = exportedSetting.Properties.First();

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null) return;

        var changedInputElement = this.GetInputElement(this._updatedXml, exportedInput);
        Assert.NotNull(changedInputElement, nameof(changedInputElement));
        var addedSettingElement = changedInputElement.GetChildren(exportedSetting.Name).SingleOrDefault();
        Assert.NotNull(addedSettingElement, nameof(addedSettingElement));
        var addedSettingValueAttribute = addedSettingElement.Attributes().First();
        var addedSettingValue = addedSettingValueAttribute.Value;
        Assert.AreEqual(exportedSetting.Properties[exportedSettingValue.Key], addedSettingValue);
    }

    [Test]
    public async Task Update_adds_input_setting_xml()
    {
        // Arrange
        var (exportedInput, exportedSetting) = this.Arrange_XmlInputSetting_MappingData();
        var (exportedSettingValueName, exportedSettingValue) = exportedSetting.Properties.Select(kvp => (kvp.Key, kvp.Value)).First();        
        // find related pre-update input node
        var targetInputElement = this.GetInputElement(this._originalXml, exportedInput);
        // find setting node
        var targetSettingElement = targetInputElement.GetChildren(exportedSetting.Name).SingleOrDefault();
        if (targetSettingElement != null)
        {
            targetSettingElement.Remove();
        }

        // Act
        await this.Act();

        // Assert
        this.AssertBasics();
        // silly code to prevent warnings
        if (this._updatedXml == null) return;

        var changedInputElement = this.GetInputElement(this._updatedXml, exportedInput);
        Assert.NotNull(changedInputElement, nameof(changedInputElement));
        var changedSettingElement = changedInputElement.GetChildren(exportedSetting.Name).SingleOrDefault();
        Assert.NotNull(changedSettingElement, nameof(changedSettingElement));
        var changedSettingValueElement = changedSettingElement.GetChildren(exportedSettingValueName).SingleOrDefault();
        Assert.NotNull(changedSettingValueElement, nameof(changedSettingValueElement));
        var changedSettingValue = changedSettingValueElement.ToString(SaveOptions.DisableFormatting);
        Assert.AreEqual(exportedSettingValue, changedSettingValue);
    }

    [Test]
    public async Task Update_restores_inputs()
    {
        // Arrange
        // scenario: target xml has joystick inputs that no longer match the preserved inputs
        // input:
        //   data-1: only joystick inputs are marked as preserve
        //   data-2: only some joystick mappings are preserved
        //   xml-1: change joystick 1->2, 2->3
        //   xml-2: change mappings js1->js2, js2->js3
        //   xml-3: preserved mappings are modified
        //   xml-4: add new joystick 1
        //   xml-5: add made-up mappings for new js1
        // expectation:
        //   assert-1: joystick inputs 1 and 2 are restored (2->1, 3->2)
        //   assert-2: made-up mappings for js1 are removed
        //   assert-3: joystick input 1 is removed
        //   assert-4: all mappings for js2 are rewritten for exported js1, ditto js3 -> js2
        //   assert-5: preserved mappings are restored

        this.Arrange_Default_MappingData();
        // data-1: only joystick inputs are marked as preserve
        this._data.Inputs.ToList().ForEach(i => i.Preserve = false);
        var exportedJoystickInputs = this._data.Inputs.Where(i => string.Equals("joystick", i.Type, StringComparison.OrdinalIgnoreCase)).ToList();
        exportedJoystickInputs.ForEach(i => i.Preserve = true);
        // data-2: only some joystick mappings are preserved
        this._data.Mappings.ToList().ForEach(m => m.Preserve = false);
        var preservedMappings = this._data.Mappings.Where(m => m.Input.StartsWith("js1_")).Take(2).Concat(this._data.Mappings.Where(m => m.Input.StartsWith("js2_")).Take(2)).ToList();
        preservedMappings.ForEach(m => m.Preserve = true);
        // xml-1: change joystick 1->2, 2->3
        var xmlsb = new StringBuilder(this._originalXml.ToString(SaveOptions.DisableFormatting));
        xmlsb.Replace("<options type=\"joystick\" instance=\"3\" />", "");
        xmlsb.Replace("<options type=\"joystick\" instance=\"2\"", "<options type=\"joystick\" instance=\"3\"");
        xmlsb.Replace("<options type=\"joystick\" instance=\"1\"", "<options type=\"joystick\" instance=\"2\"");
        // xml-2: change mappings js1->js2, js2->js3
        xmlsb.Replace("<rebind input=\"js2_", "<rebind input=\"js3_");
        xmlsb.Replace("<rebind input=\"js1_", "<rebind input=\"js2_");
        // xml-3: preserved mappings are modified
        foreach (var m in preservedMappings)
        {
            var bindInfo = m.GetInputTypeAndInstance();
            var newPrefix = $"{bindInfo.Value.Type}{bindInfo.Value.Instance+1}";
            xmlsb.Replace($"<rebind input=\"{m.Input}", $"<rebind input=\"{newPrefix}_{RandomString()}");
        }
        this._originalXml = XDocument.Parse(xmlsb.ToString());
        var targetJoystick2Mappings = this._originalXml.XPathSelectElements("//*/rebind[starts-with(@input,'js2_')]").ToList();
        var targetJoystick3Mappings = this._originalXml.XPathSelectElements("//*/rebind[starts-with(@input,'js3_')]").ToList();
        // xml-4: add new joystick 1
        var targetJoystick2Element = GetInputElement(this._originalXml, new InputDevice { Type = exportedJoystickInputs[0].Type, Instance = exportedJoystickInputs[0].Instance + 1, Product = exportedJoystickInputs[0].Product });
        var targetJoystick1Element =
            new XElement("options", 
            new XAttribute("type", "joystick"),
            new XAttribute("instance", "1"),
            new XAttribute("Product", RandomString()));
        targetJoystick2Element.AddBeforeSelf(targetJoystick1Element);
        // xml-5: add made-up mappings for new js1
        var actionProfilesElement = this._originalXml.XPathSelectElement($"/ActionMaps/ActionProfiles[@profileName='default']");
        var tempActionMapElement = new XElement("actionmap", new XAttribute("name", RandomString()));
        actionProfilesElement.Add(tempActionMapElement);
        var targetJoystick1Mappings = 
            Enumerable.Range(0, 2)
            .Select(_ => new Mapping { ActionMap = tempActionMapElement.GetAttribute("name"), Action = RandomString(), Input = $"js1_{RandomString()}" })
            .ToList();
        targetJoystick1Mappings
            .Select(m => new XElement("action", new XAttribute("name", m.Action), new XElement("rebind", new XAttribute("input", m.Input))))
            .ToList()
            .ForEach(m => tempActionMapElement.Add(m));

        // Act
        await this.Act();

        // Assert
        var updatedXmlStr = this._updatedXml.ToString(SaveOptions.DisableFormatting);
        // assert-1: joystick inputs 1 and 2 are restored (2->1, 3->2)
        foreach (var joystick in exportedJoystickInputs)
        {
            var updatedElement = this.GetInputElement(this._updatedXml, joystick);
            Assert.NotNull(updatedElement, nameof(updatedElement));
            // TODO assert that original input settings have carried over and preserved settings restored
        }
        // assert-2: made-up mappings for js1 are removed
        foreach (var m in targetJoystick1Mappings)
        {
            Assert.IsFalse(updatedXmlStr.Contains(m.Input));
        }
        // assert-3: joystick input 1 is removed
        Assert.IsFalse(updatedXmlStr.Contains(targetJoystick1Element.GetAttribute("Product")));
        // assert-4: all mappings for js2 are rewritten for exported js1, ditto js3 -> js2
        targetJoystick2Mappings.Select(e => new { ActionMap = e.Parent.Parent.GetAttribute("name"), Action = e.Parent.GetAttribute("name"), RestoredBinding = e.GetAttribute("input").Replace("js2_", "js1_") })
            .Concat(targetJoystick3Mappings.Select(e => new { ActionMap = e.Parent.Parent.GetAttribute("name"), Action = e.Parent.GetAttribute("name"), RestoredBinding = e.GetAttribute("input").Replace("js3_", "js2_") }))
            .ToList()
            .ForEach(mapping => {
                if (preservedMappings.Any(m => m.ActionMap == mapping.ActionMap && m.Action == mapping.Action)) return; // skip preserved mappings, will test later
                Assert.NotNull(this._updatedXml.XPathSelectElement($"//*/actionmap[@name='{mapping.ActionMap}']/action[@name='{mapping.Action}']/rebind[@input='{mapping.RestoredBinding}']"));
            });
        // assert-5: preserved mappings are restored
        preservedMappings
            .ForEach(m => {
                Assert.NotNull(this._updatedXml.XPathSelectElement($"//*/actionmap[@name='{m.ActionMap}']/action[@name='{m.Action}']/rebind[@input='{m.Input}']"));
            });
    }
}