using NUnit.Framework;
using SSCM.Core;
using SSCM.StarCitizen;
using static SSCM.Tests.Extensions;

namespace SSCM.StarCitizen.Tests;

public abstract class MappingImportMerger_TestBase
{
    protected readonly MappingImportMerger _merger;

    protected SCMappingData _current = new SCMappingData();
    protected SCMappingData _updated = new SCMappingData();

    protected MappingImportMerger_TestBase()
    {
        this._merger = new MappingImportMerger();
    }

    protected void Detects_All_Unchanged_Arrange()
    {
        this._current = new SCMappingData
        {
            Inputs = {
                new SCInputDevice { Type = "joystick", Instance = 1, Product = $"A{RandomString()}", Settings = { 
                    new SCInputDeviceSetting { Name = $"A{RandomString()}", Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    new SCInputDeviceSetting { Name = $"B{RandomString()}", Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    }
                },
                new SCInputDevice { Type = "joystick", Instance = 2, Product = $"B{RandomString()}", Settings = {
                    new SCInputDeviceSetting { Name = $"A{RandomString()}", Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    new SCInputDeviceSetting { Name = $"B{RandomString()}", Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    }
                },
            },
            Mappings = {
                new SCMapping { ActionMap = $"A{RandomString()}", Action = RandomString(), Input = $"js1_{RandomString()}", InputType = "joystick" },
                new SCMapping { ActionMap = $"B{RandomString()}", Action = RandomString(), Input = $"js1_{RandomString()}", InputType = "joystick" },
                new SCMapping { ActionMap = $"C{RandomString()}", Action = RandomString(), Input = $"js2_{RandomString()}", InputType = "joystick" },
                new SCMapping { ActionMap = $"D{RandomString()}", Action = RandomString(), Input = $"js2_{RandomString()}", InputType = "joystick" },
            },
            Attributes = {
                new SCAttribute { Name = $"A{RandomString()}", Value = RandomString() },
                new SCAttribute { Name = $"B{RandomString()}", Value = RandomString() },
                new SCAttribute { Name = $"C{RandomString()}", Value = RandomString() },
                new SCAttribute { Name = $"D{RandomString()}", Value = RandomString() },
            }
        };

        this._current.Inputs.Concat(this._updated.Inputs).ToList().ForEach(i => {
            i.Settings.ToList().ForEach(s => s.Parent = i.Id);
        });

        
        this._updated = this._current.JsonCopy();
    }

    protected void Detects_Inputs_Added_Arrange()
    {
        this._current = new SCMappingData
        {
            Inputs = { new SCInputDevice { Type = "joystick", Instance = 1, Product = RandomString() } }
        };
        this._updated = new SCMappingData
        {
            Inputs = { this._current.Inputs[0].JsonCopy(), new SCInputDevice { Type = "joystick", Instance = 2, Product = RandomString() } }
        };
    }

    protected void Create_2_Inputs_Arrange()
    {
        this._current = new SCMappingData
        {
            Inputs = {
                new SCInputDevice { Type = "joystick", Instance = 1, Product = Guid.NewGuid().ToString() },
                new SCInputDevice { Type = "joystick", Instance = 2, Product = Guid.NewGuid().ToString() },
            }
        };
        this._updated = new SCMappingData
        {
            Inputs = {
                this._current.Inputs[0].JsonCopy(),
                this._current.Inputs[1].JsonCopy(),
            }
        };
    }

    protected void Detects_Inputs_Removed_NotPreserved_Arrange()
    {
        this.Create_2_Inputs_Arrange();

        this._current.Mappings.Add(new SCMapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js2_{RandomString()}", InputType = "joystick", Preserve = false });
        this._updated.Inputs.RemoveAt(1);
    }

    protected void Detects_InputSettings_Added_Arrange()
    {
        this.Create_2_Inputs_Arrange();

        var updatedInput = this._updated.Inputs[0];
        updatedInput.Settings.Add(new SCInputDeviceSetting { Name = RandomString(), Parent = updatedInput.Id, Preserve = true, Properties = { { RandomString(), RandomString() } } });
    }

    protected void Detects_InputSettings_Removed_NotPreserved_Arrange()
    {
        this.Create_2_Inputs_Arrange();
        this._current.Inputs[0].Settings.Add(new SCInputDeviceSetting { Name = RandomString(), Parent = this._current.Inputs[0].Id, Preserve = false, Properties = { { "invert", "1" } } });
        this._updated.Inputs[0].Settings.Clear();
    }

    protected void Detects_InputSettings_Changed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];
        currentInput.Settings[0].Preserve = false;
        var updatedInputSettingProperties = this._updated.Inputs[0].Settings[0].Properties.First();
        updatedInput.Settings[0].Properties[updatedInputSettingProperties.Key] = RandomString();
    }

    protected void Detects_Mapping_Added_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var addedMapping = new SCMapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js1_{RandomString()}", InputType = "joystick", Preserve = true };
        this._updated.Mappings.Add(addedMapping);
    }

    protected void Detects_Mapping_Removed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var removedMapping = this._current.Mappings.Last();
        removedMapping.Preserve = false;
        this._updated.Mappings.RemoveAt(this._updated.Mappings.Count - 1);
    }

    protected void Detects_Mapping_Changed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var originalMapping = this._current.Mappings.Last();
        originalMapping.Preserve = false;
        var changedMapping = this._updated.Mappings.Last();
        changedMapping.Input = $"js1_{RandomString()}";
    }

    protected SCAttribute Detects_Attribute_Added_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var addedAttribute = new SCAttribute { Name = RandomString(), Value = RandomString() };
        this._updated.Attributes.Add(addedAttribute);
        return addedAttribute;
    }

    protected SCAttribute Detects_Attribute_Removed_NotPreserved_Arrange(bool preserve = false)
    {
        this.Detects_All_Unchanged_Arrange();
        var currentRemovedAttribute = this._current.Attributes.Last();
        currentRemovedAttribute.Preserve = preserve;
        var updatedRemovedAttribute = this._updated.Attributes.Last();
        this._updated.Attributes.Remove(updatedRemovedAttribute);
        return currentRemovedAttribute;
    }
    
    protected (SCAttribute, SCAttribute) Detects_Attribute_Changed_NotPreserved_Arrange(bool preserve = false)
    {
        this.Detects_All_Unchanged_Arrange();
        var currentChangedAttribute = this._current.Attributes.Last();
        currentChangedAttribute.Preserve = preserve;
        var updatedChangedAttribute = this._updated.Attributes.Last();
        updatedChangedAttribute.Preserve = preserve;
        updatedChangedAttribute.Value = RandomString();
        return (currentChangedAttribute, updatedChangedAttribute);
    }

    protected (SCInputDevice, SCInputDevice, SCInputDevice, SCInputDevice, 
            SCInputDeviceSetting, SCInputDeviceSetting, SCInputDeviceSetting, SCInputDeviceSetting, SCInputDeviceSetting, 
            SCMapping, SCMapping, SCMapping, SCMapping, SCMapping, SCMapping, SCMapping) Creates_Merge_Actions_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentChangingInput = this._current.Inputs[0];
        var updatedChangingInput = this._updated.Inputs[0];
        // change input setting - asserted
        var currentChangingSetting = currentChangingInput.Settings[1];
        currentChangingSetting.Preserve = false;
        var updatedChangingSetting = updatedChangingInput.Settings[1];
        updatedChangingSetting.Properties = new Dictionary<string, string> { { RandomString(), RandomString() } };
        // add input setting - asserted
        var addedSetting = new SCInputDeviceSetting { Name = $"Z{RandomString()}", Parent = updatedChangingInput.Id, Preserve = true, Properties = { { RandomString(), RandomString() } } };
        updatedChangingInput.Settings.Add(addedSetting);
        // remove input setting - asserted
        var removedSetting = currentChangingInput.Settings[0];
        removedSetting.Preserve = false;
        updatedChangingInput.Settings.RemoveAt(0);

        var currentUnchangingInput = this._current.Inputs[1];
        var updatedUnchangingInput = this._updated.Inputs[1];
        // change input setting but preserved - can't assert
        var changedSettingButPreserved = updatedUnchangingInput.Settings[1];
        currentUnchangingInput.Settings[1].Preserve = true;
        changedSettingButPreserved.Properties = new Dictionary<string, string> { { RandomString(), RandomString() } };
        // remove input setting but preserved - can't assert
        var removedSettingButPreserved = currentUnchangingInput.Settings[0];
        removedSettingButPreserved.Preserve = true;
        updatedUnchangingInput.Settings.RemoveAt(0);

        // remove input - asserted
        var removedInput = new SCInputDevice { Type = "joystick", Instance = 3, Product = RandomString() };
        this._current.Inputs.Add(removedInput);

        // add input - asserted
        var addedInput = new SCInputDevice { Type = "joystick", Instance = 4, Product = RandomString() };
        this._updated.Inputs.Add(addedInput);
        
        // change mapping and changed mapping but preserved - asserted
        var currentChangedMapping = this._current.Mappings[2];
        var updatedChangedMapping = this._updated.Mappings[2];
        updatedChangedMapping.Input = $"js2_{RandomString()}";
        var currentChangedMappingButPreserved = this._current.Mappings[3];
        currentChangedMappingButPreserved.Preserve = true;
        var updatedChangedMappingButPreserved = this._updated.Mappings[3];
        updatedChangedMappingButPreserved.Input = $"js2_{RandomString()}";
        // remove mapping and remove mapping but preserved - asserted
        var removedMapping = this._current.Mappings[0];
        var removedMappingButPreserved = this._current.Mappings[1];
        removedMappingButPreserved.Preserve = true;
        this._updated.Mappings.RemoveAt(1);
        this._updated.Mappings.RemoveAt(0);
        // add mapping - asserted
        var addedMapping = new SCMapping { ActionMap = $"Z{RandomString()}", Action = RandomString(), Input = $"js1_{RandomString()}", InputType = "joystick", Preserve = true };
        this._updated.Mappings.Add(addedMapping);

        return (addedInput, removedInput, currentChangingInput, updatedChangingInput, 
            addedSetting, removedSetting, removedSettingButPreserved, updatedChangingSetting, changedSettingButPreserved,
            addedMapping, removedMapping, removedMappingButPreserved, currentChangedMapping, updatedChangedMapping, currentChangedMappingButPreserved, updatedChangedMappingButPreserved);
    }
}