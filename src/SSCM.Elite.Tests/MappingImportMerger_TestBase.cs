using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using static SSCM.Tests.Extensions;

namespace SSCM.Elite.Tests;

#pragma warning disable CS8602

public abstract class MappingImportMerger_TestBase
{
    protected readonly MappingImportMerger _merger;

    protected EDMappingData _current = new EDMappingData();
    protected EDMappingData _updated = new EDMappingData();

    protected MappingImportMerger_TestBase()
    {
        this._merger = new MappingImportMerger();
    }

    protected void Detects_All_Unchanged_Arrange()
    {
        this._current = new EDMappingData
        {
            Settings = {
                new EDMappingSetting("Ship-MouseControls", "MouseXMode", "Bindings_MouseRoll")
            },
            Mappings = {
                new EDMapping("Ship-Cooling", "ToggleButtonUpInput") {
                    Primary = new EDBinding("Keyboard", "Key_V"), 
                    Secondary = new EDBinding("231D3205", "Joy_15", new[] { new EDBindingKey("231D3205", "Joy_1"), new EDBindingKey("231D3205", "Joy_2") }),
                    Settings = {
                        new EDMappingSetting("Ship-Cooling.ToggleButtonUpInput", "ToggleOn", "1"),
                    }
                },
                new EDMapping("Ship-Weapons", "CycleFireGroupPrevious") { 
                    Primary = EDBinding.UNBOUND(), 
                    Secondary = new EDBinding("231D0200", "Joy_22") },
                new EDMapping("Ship-Throttle", "ForwardKey") { 
                    Primary = new EDBinding("Keyboard", "Key_W"), 
                    Secondary = new EDBinding("231D3205", "Joy_POV1Up") },
                new EDMapping("Ship-FlightRotation", "PitchAxisRaw") { 
                    Binding = new EDBinding("231D0200", "Joy_YAxis"), 
                    Settings = { 
                        new EDMappingSetting("Ship-FlightRotation.PitchAxisRaw", "Deadzone", "0.00000000"), 
                        new EDMappingSetting("Ship-FlightRotation.PitchAxisRaw", "Inverted", "1") 
                    }
                },
            }
        };

        this._current.Mappings.SelectMany(m => new[] { m.Binding, m.Primary, m.Secondary }).ToList().ForEach(m => { if (m != null) m.Preserve = false; });
        this._current.Settings.Concat(this._current.Mappings.SelectMany(m => m.Settings)).ToList().ForEach(s => { s.Preserve = false; });

        this._updated = this._current.JsonCopy();
    }

    protected EDMapping Detects_Mapping_Added_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var added = new EDMapping("Ship-Throttle", RandomString()) { Binding = new EDBinding(RandomString(), RandomString()) };
        this._updated.Mappings.Add(added);
        return added;
    }

    protected EDMapping Detects_Mapping_Removed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var removed = this._updated.Mappings.Single(m => m.Id == "Ship-Throttle.ForwardKey");
        this._updated.Mappings.Remove(removed);
        var removedCurrent = this._current.Mappings.Single(m => m.Id == removed.Id);
        removedCurrent.Primary.Preserve = false;
        removedCurrent.Secondary.Preserve = false;
        return removedCurrent;
    }

    protected EDMapping Detects_Mapping_Removed_Preserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var removed = this.Detects_Mapping_Removed_NotPreserved_Arrange();
        var removedCurrent = this._current.Mappings.Single(m => m.Id == removed.Id);
        removedCurrent.Primary.Preserve = true;
        removedCurrent.Secondary.Preserve = true;
        return removedCurrent;
    }

    protected (EDMapping, EDMapping) Detects_MappingBinding_Changed_NotPreserved_Arrange(
        string mappingId = "Ship-Weapons.CycleFireGroupPrevious", 
        string type = nameof(EDMapping.Primary),
        bool preserve = false)
    {
        this.Detects_All_Unchanged_Arrange();
        var changed = this._updated.Mappings.Single(m => m.Id == mappingId);
        var current = this._current.Mappings.Single(m => m.Id == changed.Id);
        var changedBinding = changed.GetBinding(type);
        changedBinding.Key.Device = RandomString();
        changedBinding.Key.Key = RandomString();
        current.GetBinding(type).Preserve = preserve;
        return (current, changed);
    }

    protected (EDMapping, EDMapping) Detects_MappingBinding_Changed_Preserved_Arrange(
        string mappingId = "Ship-Weapons.CycleFireGroupPrevious", 
        string type = nameof(EDMapping.Primary))
    {
        return this.Detects_MappingBinding_Changed_NotPreserved_Arrange(mappingId, type, true);
    }

    protected (EDMapping, EDMapping) Detects_MappingBinding_Both_Changed_NotPreserved_Arrange()
    {
        var mappingId = "Ship-Weapons.CycleFireGroupPrevious";
        this.Detects_All_Unchanged_Arrange();
        var changed = this._updated.Mappings.Single(m => m.Id == mappingId);
        var current = this._current.Mappings.Single(m => m.Id == changed.Id);
        changed.Primary.Key.Device = RandomString();
        changed.Primary.Key.Key = RandomString();
        current.Primary.Preserve = false;
        changed.Secondary.Key.Device = RandomString();
        changed.Secondary.Key.Key = RandomString();
        current.Secondary.Preserve = false;
        return (current, changed);
    }

    protected (EDMapping, EDMappingSetting) Detects_MappingSetting_Added_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var mapping = this._updated.Mappings.Single(m => m.Id == "Ship-FlightRotation.PitchAxisRaw");
        var addedSetting = new EDMappingSetting(mapping.Id, RandomString(), RandomString());
        mapping.Settings.Add(addedSetting);
        return (mapping, addedSetting);
    }

    protected (EDMapping, EDMappingSetting) Detects_MappingSetting_Removed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentMapping = this._current.Mappings.Single(m => m.Id == "Ship-FlightRotation.PitchAxisRaw");
        var updatedMapping = this._updated.Mappings.Single(m => m.Id == "Ship-FlightRotation.PitchAxisRaw");
        var currentSetting = currentMapping.Settings.Single(s => s.Name == "Deadzone");
        currentSetting.Preserve = false;
        var updatedSetting = updatedMapping.Settings.Single(s => s.Name == "Deadzone");
        updatedMapping.Settings.Remove(updatedSetting);
        return (currentMapping, currentSetting);
    }

    protected (EDMapping, EDMappingSetting) Detects_MappingSetting_Removed_Preserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentMapping = this._current.Mappings.Single(m => m.Id == "Ship-FlightRotation.PitchAxisRaw");
        var updatedMapping = this._updated.Mappings.Single(m => m.Id == "Ship-FlightRotation.PitchAxisRaw");
        var currentSetting = currentMapping.Settings.Single(s => s.Name == "Deadzone");
        currentSetting.Preserve = true;
        var updatedSetting = updatedMapping.Settings.Single(s => s.Name == "Deadzone");
        updatedMapping.Settings.Remove(updatedSetting);
        return (currentMapping, currentSetting);
    }

    protected (EDMapping, EDMappingSetting, EDMappingSetting) Detects_MappingSetting_Changed_NotPreserved_Arrange(bool preserve = false)
    {
        this.Detects_All_Unchanged_Arrange();
        var currentMapping = this._current.Mappings.Single(m => m.Id == "Ship-FlightRotation.PitchAxisRaw");
        var updatedMapping = this._updated.Mappings.Single(m => m.Id == "Ship-FlightRotation.PitchAxisRaw");
        var currentSetting = currentMapping.Settings.Single(s => s.Name == "Deadzone");
        currentSetting.Preserve = preserve;
        var updatedSetting = updatedMapping.Settings.Single(s => s.Name == "Deadzone");
        updatedSetting.Value = RandomString();
        return (currentMapping, currentSetting, updatedSetting);
    }

    protected (EDMapping, EDMappingSetting, EDMappingSetting) Detects_MappingSetting_Changed_Preserved_Arrange()
    {
        return this.Detects_MappingSetting_Changed_NotPreserved_Arrange(true);
    }

    protected EDMappingSetting Detects_Setting_Added_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var mapping = this._updated.Mappings.First(); // get the first mapping just to copy its group
        var addedSetting = new EDMappingSetting(mapping.Group, RandomString(), RandomString());
        mapping.Settings.Add(addedSetting);
        return addedSetting;
    }

    protected EDMappingSetting Detects_Setting_Removed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentSetting = this._current.Settings.Single(s => s.Id == "Ship-MouseControls.MouseXMode");
        currentSetting.Preserve = false;
        var updatedSetting = this._updated.Settings.Single(s => s.Id == "Ship-MouseControls.MouseXMode");
        this._updated.Settings.Remove(updatedSetting);
        return currentSetting;
    }

    protected EDMappingSetting Detects_Setting_Removed_Preserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentSetting = this._current.Settings.Single(s => s.Id == "Ship-MouseControls.MouseXMode");
        currentSetting.Preserve = true;
        var updatedSetting = this._updated.Settings.Single(s => s.Id == "Ship-MouseControls.MouseXMode");
        this._updated.Settings.Remove(updatedSetting);
        return currentSetting;
    }

    protected (EDMappingSetting, EDMappingSetting) Detects_Setting_Changed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentSetting = this._current.Settings.Single(s => s.Id == "Ship-MouseControls.MouseXMode");
        currentSetting.Preserve = false;
        var updatedSetting = this._updated.Settings.Single(s => s.Id == "Ship-MouseControls.MouseXMode");
        updatedSetting.Value = RandomString();
        return (currentSetting, updatedSetting);
    }

    protected (EDMappingSetting, EDMappingSetting) Detects_Setting_Changed_Preserved_Arrange()
    {
        var (currentSetting, updatedSetting) = this.Detects_Setting_Changed_NotPreserved_Arrange();
        currentSetting.Preserve = true;
        return (currentSetting, updatedSetting);
    }

    // protected void Creates_Merge_Actions_Arrange()
    // {
    //     this.Detects_All_Unchanged_Arrange();
        
    //     // some massive comprehensive test that i'm not going to do right now
    // }
}
