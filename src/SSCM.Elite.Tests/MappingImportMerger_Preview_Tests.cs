using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using static SSCM.Tests.Extensions;

namespace SSCM.Elite.Tests;

[TestFixture]
public class MappingImportMerger_Preview_Tests : MappingImportMerger_TestBase
{
    private bool _result;

    public MappingImportMerger_Preview_Tests()
    {
    }

    private bool Act()
    {
        return this._result = this._merger.Preview(this._current, this._updated);
    }

    [Test]
    public void Detects_All_Unchanged()
    {
        // Arrange
        base.Detects_All_Unchanged_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }

    [Test]
    public void Detects_Mapping_Added()
    {
        // Arrange
        var addedMapping = this.Detects_Mapping_Added_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultED.MappingDiffs.Added.Count);
        AssertED.AreEqual(addedMapping, this._merger.ResultED.MappingDiffs.Added[0]);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedMapping), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_Mapping_Removed_NotPreserved()
    {
        // Arrange
        var removedMapping = this.Detects_Mapping_Removed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultED.MappingDiffs.Removed.Count);
        AssertED.AreEqual(removedMapping, this._merger.ResultED.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_Mapping_Removed_Preserved()
    {
        // Arrange
        var removedMapping = this.Detects_Mapping_Removed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultED.MappingDiffs.Removed.Count);
        AssertED.AreEqual(removedMapping, this._merger.ResultED.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_MappingBinding_Changed_NotPreserved()
    {
        // Arrange
        var (current, updated) = this.Detects_MappingBinding_Changed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.MappingDiffs.Changed.Count);
        AssertED.AreEqual(current, this._merger.ResultED.MappingDiffs.Changed[0].Current);
        AssertED.AreEqual(updated, this._merger.ResultED.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, (updated, "Primary")), this._merger.ResultED.MergeActions[0], 
            (e, a) => {
                var et = ((EDMapping mapping, string type)) e;
                var at = ((EDMapping mapping, string type)) a;
                Assert.AreSame(et.mapping, at.mapping, nameof(et.mapping));
                Assert.AreEqual(et.type, at.type, nameof(et.type));
            });
    }
    
    [Test]
    public void Detects_MappingBinding_Changed_Preserved()
    {
        // Arrange
        var (current, updated) = this.Detects_MappingBinding_Changed_Preserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.MappingDiffs.Changed.Count);
        AssertED.AreEqual(current, this._merger.ResultED.MappingDiffs.Changed[0].Current);
        AssertED.AreEqual(updated, this._merger.ResultED.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }

    [Test]
    public void Detects_MappingSetting_Added()
    {
        // Arrange
        var added = this.Detects_MappingSetting_Added_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Added.Count);
        AssertED.AreEqual(added, this._merger.ResultED.SettingDiffs.Added[0]);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, added), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_MappingSetting_Removed_NotPreserved()
    {
        // Arrange
        var removed = this.Detects_MappingSetting_Removed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Removed.Count);
        AssertED.AreEqual(removed, this._merger.ResultED.SettingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removed), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_MappingSetting_Removed_Preserved()
    {
        // Arrange
        var removed = this.Detects_MappingSetting_Removed_Preserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Removed.Count);
        AssertED.AreEqual(removed, this._merger.ResultED.SettingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }


    [Test]
    public void Detects_MappingSetting_Changed_NotPreserved()
    {
        // Arrange
        var (current, updated) = this.Detects_MappingSetting_Changed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Changed.Count);
        AssertED.AreEqual(current, this._merger.ResultED.SettingDiffs.Changed[0].Current);
        AssertED.AreEqual(updated, this._merger.ResultED.SettingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updated), this._merger.ResultED.MergeActions[0]);
    }
    
    [Test]
    public void Detects_MappingSetting_Changed_Preserved()
    {
        // Arrange
        var (current, updated) = this.Detects_MappingSetting_Changed_Preserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Changed.Count);
        AssertED.AreEqual(current, this._merger.ResultED.SettingDiffs.Changed[0].Current);
        AssertED.AreEqual(updated, this._merger.ResultED.SettingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }

    [Test]
    public void Detects_Setting_Added()
    {
        // Arrange
        var added = this.Detects_Setting_Added_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Added.Count);
        AssertED.AreEqual(added, this._merger.ResultED.SettingDiffs.Added[0]);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, added), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_Setting_Removed_NotPreserved()
    {
        // Arrange
        var removed = this.Detects_Setting_Removed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Removed.Count);
        AssertED.AreEqual(removed, this._merger.ResultED.SettingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removed), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_Setting_Removed_Preserved()
    {
        // Arrange
        var removed = this.Detects_Setting_Removed_Preserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Removed.Count);
        AssertED.AreEqual(removed, this._merger.ResultED.SettingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }


    [Test]
    public void Detects_Setting_Changed_NotPreserved()
    {
        // Arrange
        var (current, updated) = this.Detects_Setting_Changed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Changed.Count);
        AssertED.AreEqual(current, this._merger.ResultED.SettingDiffs.Changed[0].Current);
        AssertED.AreEqual(updated, this._merger.ResultED.SettingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updated), this._merger.ResultED.MergeActions[0]);
    }
    
    [Test]
    public void Detects_Setting_Changed_Preserved()
    {
        // Arrange
        var (current, updated) = this.Detects_Setting_Changed_Preserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.SettingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.SettingDiffs.Changed.Count);
        AssertED.AreEqual(current, this._merger.ResultED.SettingDiffs.Changed[0].Current);
        AssertED.AreEqual(updated, this._merger.ResultED.SettingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }

    // [Test]
    // public void Creates_Merge_Actions()
    // {
    //     // Arrange

    //     // Act
    //     var canMerge = this.Act();

    //     // Assert
    //     Assert.IsTrue(canMerge);
    // }
}