using NUnit.Framework;
using SCCM.Core;
using static SCCM.Tests.Extensions;

namespace SCCM.Tests;

public abstract class MappingImportMerger_TestBase
{
    protected readonly MappingImportMerger _merger;

    protected MappingData _current = new MappingData();
    protected MappingData _updated = new MappingData();

    protected MappingImportMerger_TestBase()
    {
        this._merger = new MappingImportMerger();
    }

    protected void Detects_All_Unchanged_Arrange()
    {
        this._current = new MappingData
        {
            Inputs = {
                new InputDevice { Type = "joystick", Instance = 1, Product = RandomString(), Settings = { 
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    }
                },
                new InputDevice { Type = "joystick", Instance = 2, Product = RandomString(), Settings = {
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    }
                },
            },
            Mappings = {
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js1_{RandomString()}" },
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js1_{RandomString()}" },
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js2_{RandomString()}" },
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js2_{RandomString()}" },
            }
        };
        this._updated = new MappingData
        {
            Inputs = {
                this._current.Inputs[0].JsonCopy(),
                this._current.Inputs[1].JsonCopy(),
            },
            Mappings = {
                this._current.Mappings[0].JsonCopy(),
                this._current.Mappings[1].JsonCopy(),
                this._current.Mappings[2].JsonCopy(),
                this._current.Mappings[3].JsonCopy(),
            }
        };

        this._current.Inputs.Concat(this._updated.Inputs).ToList().ForEach(i => {
            i.Settings.ToList().ForEach(s => s.Parent = i.Id);
        });
    }

    protected void Detects_Inputs_Added_Arrange()
    {
        this._current = new MappingData
        {
            Inputs = { new InputDevice { Type = "joystick", Instance = 1, Product = RandomString() } }
        };
        this._updated = new MappingData
        {
            Inputs = { this._current.Inputs[0].JsonCopy(), new InputDevice { Type = "joystick", Instance = 2, Product = RandomString() } }
        };
    }

    protected void Create_2_Inputs_Arrange()
    {
        this._current = new MappingData
        {
            Inputs = {
                new InputDevice { Type = "joystick", Instance = 1, Product = Guid.NewGuid().ToString() },
                new InputDevice { Type = "joystick", Instance = 2, Product = Guid.NewGuid().ToString() },
            }
        };
        this._updated = new MappingData
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

        this._current.Mappings.Add(new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js2_{RandomString()}", Preserve = false });
        this._updated.Inputs.RemoveAt(1);
    }

    protected void Detects_InputSettings_Added_Arrange()
    {
        this.Create_2_Inputs_Arrange();

        var updatedInput = this._updated.Inputs[0];
        updatedInput.Settings.Add(new InputDeviceSetting { Name = RandomString(), Parent = updatedInput.Id, Preserve = true, Properties = { { RandomString(), RandomString() } } });
    }

    protected void Detects_InputSettings_Removed_NotPreserved_Arrange()
    {
        this.Create_2_Inputs_Arrange();
        this._current.Inputs[0].Settings.Add(new InputDeviceSetting { Name = RandomString(), Parent = this._current.Inputs[0].Id, Preserve = false, Properties = { { "invert", "1" } } });
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
        var addedMapping = new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js1_{RandomString()}", Preserve = true };
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

    protected (InputDevice, InputDevice, InputDevice, InputDevice, 
            InputDeviceSetting, InputDeviceSetting, InputDeviceSetting, 
            Mapping, Mapping, Mapping, Mapping, Mapping, Mapping, Mapping) Creates_Merge_Actions_Arrange()
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
        var addedSetting = new InputDeviceSetting { Name = RandomString(), Parent = updatedChangingInput.Id, Preserve = true, Properties = { { RandomString(), RandomString() } } };
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
        var removedInput = new InputDevice { Type = "joystick", Instance = 3, Product = RandomString() };
        this._current.Inputs.Add(removedInput);

        // add input - asserted
        var addedInput = new InputDevice { Type = "joystick", Instance = 4, Product = RandomString() };
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
        var addedMapping = new Mapping { ActionMap = RandomString(), Action = RandomString(), Preserve = true, Input = $"js1_{RandomString()}" };
        this._updated.Mappings.Add(addedMapping);

        return (addedInput, removedInput, currentChangingInput, updatedChangingInput, 
            addedSetting, removedSetting, updatedChangingSetting, 
            addedMapping, removedMapping, removedMappingButPreserved, currentChangedMapping, updatedChangedMapping, currentChangedMappingButPreserved, updatedChangedMappingButPreserved);
    }
}