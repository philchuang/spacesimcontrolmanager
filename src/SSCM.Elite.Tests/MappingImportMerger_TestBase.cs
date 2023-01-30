using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite;
using static SSCM.Tests.Extensions;

namespace SSCM.Elite.Tests;

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
                        new EDMappingSetting("Ship-Cooling-ToggleButtonUpInput", "ToggleOn", "1"),
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
                        new EDMappingSetting("Ship-FlightRotation-PitchAxisRaw", "Deadzone", "0.00000000"), 
                        new EDMappingSetting("Ship-FlightRotation-PitchAxisRaw", "Inverted", "1") 
                    }
                },
            }
        };
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
        var removed = this._updated.Mappings.Single(m => m.Id == "Ship-Throttle-ForwardKey");
        this._updated.Mappings.Remove(removed);
        var removedCurrent = this._current.Mappings.Single(m => m.Id == removed.Id);
        removedCurrent.Primary!.Preserve = false;
        removedCurrent.Secondary!.Preserve = false;
        return removedCurrent;
    }

    protected EDMapping Detects_Mapping_Removed_Preserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var removed = this.Detects_Mapping_Removed_NotPreserved_Arrange();
        var removedCurrent = this._current.Mappings.Single(m => m.Id == removed.Id);
        removedCurrent.Primary!.Preserve = true;
        removedCurrent.Secondary!.Preserve = true;
        return removedCurrent;
    }

    protected (EDMapping, EDMapping) Detects_MappingBinding_Changed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var changed = this._updated.Mappings.Single(m => m.Id == "Ship-Weapons-CycleFireGroupPrevious");
        var changedCurrent = this._current.Mappings.Single(m => m.Id == changed.Id);
        changed.Primary!.Key.Device = RandomString();
        changed.Primary!.Key.Key = RandomString();
        changedCurrent.Primary!.Preserve = false;
        return (changedCurrent, changed);
    }

    protected (EDMapping, EDMapping) Detects_MappingBinding_Changed_Preserved_Arrange()
    {
        var (changedCurrent, changed) = this.Detects_MappingBinding_Changed_NotPreserved_Arrange();
        changedCurrent.Primary!.Preserve = true;
        return (changedCurrent, changed);
    }
}