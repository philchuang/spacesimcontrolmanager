using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using static SSCM.Tests.Extensions;

namespace SSCM.Elite.Tests;

#pragma warning disable CS8602

[TestFixture]
public class MappingImportMerger_Merge_Tests : MappingImportMerger_TestBase
{
    private EDMappingData? _original;

    private void Act()
    {
        this._original = this._current.JsonCopy();
        this._merger.Merge(this._current, this._updated);
    }


    [Test]
    public void Does_Not_Change_With_No_Changes()
    {
        // Arrange
        base.Detects_All_Unchanged_Arrange();

        // Act
        this.Act();

        // Assert
        AssertED.AreEqual(this._original, this._current);
    }

    [Test]
    public void Mapping_Added()
    {
        // Arrange
        var addedMapping = this.Detects_Mapping_Added_Arrange();

        // Act
        this.Act();

        // Assert
        // check addedMapping is not in original
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == addedMapping.Name);
        Assert.IsNull(originalMapping, nameof(originalMapping));
        // check addedMapping is in final
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == addedMapping.Name);
        Assert.NotNull(finalMapping, nameof(finalMapping));
        AssertED.AreEqual(addedMapping, finalMapping);
    }

    [Test]
    public void Mapping_Removed()
    {
        // Arrange
        var removedMapping = this.Detects_Mapping_Removed_NotPreserved_Arrange();

        // Act
        this.Act();

        // Assert
        // check removedMapping is in original
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == removedMapping.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        // check removedMapping is not in final
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == removedMapping.Name);
        Assert.IsNull(finalMapping, nameof(finalMapping));
    }

    [Test]
    public void Mapping_NotRemoved()
    {
        // Arrange
        var removedMapping = this.Detects_Mapping_Removed_Preserved_Arrange();

        // Act
        this.Act();

        // Assert
        // check removedMapping is in original
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == removedMapping.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        // check removedMapping is still in final
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == removedMapping.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        AssertED.AreEqual(originalMapping, finalMapping);
    }

    [Test]
    [TestCase("Ship-Weapons.CycleFireGroupPrevious", nameof(EDMapping.Primary))]
    [TestCase("Ship-Weapons.CycleFireGroupPrevious", nameof(EDMapping.Secondary))]
    [TestCase("Ship-FlightRotation.PitchAxisRaw", nameof(EDMapping.Binding))]
    public void MappingBinding_Changed(string mappingId, string type)
    {
        // Arrange
        var (current, updated) = this.Detects_MappingBinding_Changed_NotPreserved_Arrange(mappingId, type);

        // Act
        this.Act();

        // Assert
        // check original mapping exists
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == current.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        // check current mapping is updated
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == current.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        Assert.Throws<AssertionException>(() => AssertED.AreEqual(originalMapping.GetBinding(type), finalMapping.GetBinding(type)));
    }
    
    [Test]
    [TestCase("Ship-Weapons.CycleFireGroupPrevious", nameof(EDMapping.Primary))]
    [TestCase("Ship-Weapons.CycleFireGroupPrevious", nameof(EDMapping.Secondary))]
    [TestCase("Ship-FlightRotation.PitchAxisRaw", nameof(EDMapping.Binding))]
    public void MappingBinding_NotChanged(string mappingId, string type)
    {
        // Arrange
        var (current, updated) = this.Detects_MappingBinding_Changed_Preserved_Arrange(mappingId, type);

        // Act
        this.Act();

        // Assert
        // check original mapping exists
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == current.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        // check current mapping is not changed
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == current.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        AssertED.AreEqual(originalMapping.GetBinding(type), finalMapping.GetBinding(type));
    }

    [Test]
    public void MappingBindings_Changed()
    {
        // Arrange
        var (current, updated) = this.Detects_MappingBinding_Both_Changed_NotPreserved_Arrange();

        // Act
        this.Act();

        // Assert
        // check original mapping exists
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == current.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        // check current mapping is updated
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == current.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        Assert.Throws<AssertionException>(() => AssertED.AreEqual(originalMapping.Primary, finalMapping.Primary));
        Assert.Throws<AssertionException>(() => AssertED.AreEqual(originalMapping.Secondary, finalMapping.Secondary));
    }

    [Test]
    public void MappingSetting_Added()
    {
        // Arrange
        var (mapping, addedSetting) = this.Detects_MappingSetting_Added_Arrange();

        // Act
        this.Act();

        // Assert
        // check addedSetting is not in original
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        var originalSetting = originalMapping.Settings.SingleOrDefault(s => s.Name == addedSetting.Name);
        Assert.IsNull(originalSetting, nameof(originalSetting));
        // check addedSetting is in final
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        var finalSetting = finalMapping.Settings.SingleOrDefault(s => s.Name == addedSetting.Name);
        Assert.NotNull(finalSetting, nameof(finalSetting));
        AssertED.AreEqual(addedSetting, finalSetting);
    }

    [Test]
    public void MappingSetting_Removed()
    {
        // Arrange
        var (mapping, removedSetting) = this.Detects_MappingSetting_Removed_NotPreserved_Arrange();

        // Act
        this.Act();

        // Assert
        // check removedSetting is in original
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        var originalSetting = originalMapping.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // check removedSetting is not in final
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        var finalSetting = finalMapping.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNull(finalSetting, nameof(finalSetting));
    }

    [Test]
    public void MappingSetting_NotRemoved()
    {
        // Arrange
        var (mapping, removedSetting) = this.Detects_MappingSetting_Removed_Preserved_Arrange();

        // Act
        this.Act();

        // Assert
        // check removedSetting is in original
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        var originalSetting = originalMapping.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // check removedSetting is in final
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        var finalSetting = finalMapping.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNotNull(finalSetting, nameof(finalSetting));
        AssertED.AreEqual(originalSetting, finalSetting);
    }


    [Test]
    public void MappingSetting_Changed()
    {
        // Arrange
        var (mapping, currentSetting, updatedSetting) = this.Detects_MappingSetting_Changed_NotPreserved_Arrange();

        // Act
        this.Act();

        // Assert
        // get original setting
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        var originalSetting = originalMapping.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // get final setting
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        var finalSetting = finalMapping.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(finalSetting, nameof(finalSetting));
        // assert different from original
        Assert.AreNotEqual(originalSetting.Value, finalSetting.Value);
        // assert same as updated
        Assert.AreEqual(updatedSetting.Value, finalSetting.Value);
    }
    
    [Test]
    public void MappingSetting_NotChanged()
    {
        // Arrange
        var (mapping, currentSetting, updatedSetting) = this.Detects_MappingSetting_Changed_Preserved_Arrange();

        // Act
        this.Act();

        // Assert
        // get original setting
        var originalMapping = this._original.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(originalMapping, nameof(originalMapping));
        var originalSetting = originalMapping.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // get final setting
        var finalMapping = this._current.Mappings.SingleOrDefault(m => m.Name == mapping.Name);
        Assert.IsNotNull(finalMapping, nameof(finalMapping));
        var finalSetting = finalMapping.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(finalSetting, nameof(finalSetting));
        // assert same as original
        Assert.AreEqual(originalSetting.Value, finalSetting.Value);
        // assert different from updated
        Assert.AreNotEqual(updatedSetting.Value, finalSetting.Value);
    }

    [Test]
    public void Setting_Added()
    {
        // Arrange
        var addedSetting = this.Detects_Setting_Added_Arrange();

        // Act
        this.Act();

        // Assert
        // check addedSetting is not in original
        var originalSetting = this._original.Settings.SingleOrDefault(s => s.Name == addedSetting.Name);
        Assert.IsNull(originalSetting, nameof(originalSetting));
        // check addedSetting is in final
        var finalSetting = this._current.Settings.SingleOrDefault(s => s.Name == addedSetting.Name);
        Assert.NotNull(finalSetting, nameof(finalSetting));
        AssertED.AreEqual(addedSetting, finalSetting);
    }

    [Test]
    public void Setting_Removed()
    {
        // Arrange
        var removedSetting = this.Detects_Setting_Removed_NotPreserved_Arrange();

        // Act
        this.Act();

        // Assert
        // check removedSetting is in original
        var originalSetting = this._original.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // check removedSetting is not in final
        var finalSetting = this._current.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNull(finalSetting, nameof(finalSetting));
    }

    [Test]
    public void Setting_NotRemoved()
    {
        // Arrange
        var removedSetting = this.Detects_Setting_Removed_Preserved_Arrange();

        // Act
        this.Act();

        // Assert
        // check removedSetting is in original
        var originalSetting = this._original.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // check removedSetting is in final
        var finalSetting = this._current.Settings.SingleOrDefault(s => s.Name == removedSetting.Name);
        Assert.IsNotNull(finalSetting, nameof(finalSetting));
        AssertED.AreEqual(originalSetting, finalSetting);
    }


    [Test]
    public void Setting_Changed()
    {
        // Arrange
        var (currentSetting, updatedSetting) = this.Detects_Setting_Changed_NotPreserved_Arrange();

        // Act
        this.Act();

        // Assert
        // get original setting
        var originalSetting = this._original.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // get final setting
        var finalSetting = this._current.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(finalSetting, nameof(finalSetting));
        // assert different from original
        Assert.AreNotEqual(originalSetting.Value, finalSetting.Value);
        // assert same as updated
        Assert.AreEqual(updatedSetting.Value, finalSetting.Value);
    }
    
    [Test]
    public void Setting_NotChanged()
    {
        // Arrange
        var (currentSetting, updatedSetting) = this.Detects_Setting_Changed_Preserved_Arrange();

        // Act
        this.Act();

        // Assert
        // get original setting
        var originalSetting = this._original.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(originalSetting, nameof(originalSetting));
        // get final setting
        var finalSetting = this._current.Settings.SingleOrDefault(s => s.Name == updatedSetting.Name);
        Assert.IsNotNull(finalSetting, nameof(finalSetting));
        // assert same as original
        Assert.AreEqual(originalSetting.Value, finalSetting.Value);
        // assert different from updated
        Assert.AreNotEqual(updatedSetting.Value, finalSetting.Value);
    }
}