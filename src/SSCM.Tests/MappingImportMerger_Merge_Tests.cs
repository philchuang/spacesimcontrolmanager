using NUnit.Framework;
using SSCM.Core;

namespace SSCM.Tests;

[TestFixture]
public class MappingImportMerger_Merge_Tests : MappingImportMerger_TestBase
{
    private MappingData _result = new MappingData();
    private MappingData _expected = new MappingData();

    public MappingImportMerger_Merge_Tests()
    {
    }

    private MappingData Act()
    {
        return this._result = this._merger.Merge(this._current, this._updated);
    }

    private void Assert()
    {
        AssertSscm.AreEqual(this._expected, this._result);
    }

    [Test]
    public void Adds_Input()
    {
        // Arrange
        this.Detects_Inputs_Added_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Removes_Input()
    {
        // Arrange
        this.Detects_Inputs_Removed_NotPreserved_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Adds_InputSetting()
    {
         // Arrange
        this.Detects_InputSettings_Added_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Removes_InputSetting()
    {
         // Arrange
        this.Detects_InputSettings_Removed_NotPreserved_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Changes_InputSetting()
    {
        // Arrange
        this.Detects_InputSettings_Changed_NotPreserved_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
            Mappings = this._updated.Mappings.Select(m => m.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Adds_Mapping()
    {
         // Arrange
        this.Detects_Mapping_Added_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
            Mappings = this._updated.Mappings.Select(m => m.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Removes_Mapping()
    {
         // Arrange
        this.Detects_Mapping_Removed_NotPreserved_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
            Mappings = this._updated.Mappings.Select(m => m.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Changes_Mapping()
    {
        // Arrange
        this.Detects_Mapping_Changed_NotPreserved_Arrange();
        this._expected = new MappingData {
            Inputs = this._updated.Inputs.Select(i => i.JsonCopy()).ToList(),
            Mappings = this._updated.Mappings.Select(m => m.JsonCopy()).ToList(),
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }

    [Test]
    public void Merges_Everything()
    {
        // Arrange
        var (addedInput, removedInput, currentChangingInput, updatedChangingInput, 
            addedSetting, removedSetting, updatedChangingSetting, 
            addedMapping, removedMapping, removedMappingButPreserved, currentChangedMapping, updatedChangedMapping, currentChangedMappingButPreserved, updatedChangedMappingButPreserved) = this.Creates_Merge_Actions_Arrange();
        this._expected = new MappingData {
            Inputs = {
                this._updated.Inputs[0].JsonCopy(),
                this._current.Inputs[1].JsonCopy(),
                addedInput.JsonCopy(),
            },
            Mappings = {
                removedMappingButPreserved.JsonCopy(),
                updatedChangedMapping.JsonCopy(),
                currentChangedMappingButPreserved.JsonCopy(),
                addedMapping.JsonCopy(),
            }
        };

        // Act
        this.Act();

        // Assert
        this.Assert();
    }
}