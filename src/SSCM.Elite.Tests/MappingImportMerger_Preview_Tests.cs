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

    /*
    [Test]
    public void Detects_MappingSetting_Added_NotPreserved()
    {
        Assert.Fail();
    }

    [Test]
    public void Detects_MappingSetting_Added_Preserved()
    {
        Assert.Fail();
    }

    [Test]
    public void Detects_MappingSetting_Removed_NotPreserved()
    {
        Assert.Fail();
    }

    [Test]
    public void Detects_MappingSetting_Removed_Preserved()
    {
        Assert.Fail();
    }

    [Test]
    public void Detects_MappingSetting_Changed_NotPreserved()
    {
        Assert.Fail();
    }

    [Test]
    public void Detects_MappingSetting_Changed_Preserved()
    {
        Assert.Fail();
    }

    [Test]
    public void Detects_Settings_Added()
    {
        // Arrange
        this.Detects_Settings_Added_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.InputDiffs.Changed.Count);
        AssertED.AreEqual(currentInput, this._merger.ResultED.InputDiffs.Changed[0].Current);
        AssertED.AreEqual(updatedInput, this._merger.ResultED.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, updatedInput.Settings[0]), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_Settings_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_Settings_Removed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.InputDiffs.Changed.Count);
        AssertED.AreEqual(currentInput, this._merger.ResultED.InputDiffs.Changed[0].Current);
        AssertED.AreEqual(updatedInput, this._merger.ResultED.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, currentInput.Settings[0]), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_Settings_Removed_Preserved()
    {
        // Arrange
        this.Detects_Settings_Removed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];
        currentInput.Settings[0].Preserve = true;

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.InputDiffs.Changed.Count);
        AssertED.AreEqual(currentInput, this._merger.ResultED.InputDiffs.Changed[0].Current);
        AssertED.AreEqual(updatedInput, this._merger.ResultED.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }

    [Test]
    public void Detects_Settings_Changed_NotPreserved()
    {
        // Arrange
        this.Detects_Settings_Changed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var canMerge = this.Act();

        // Arrange
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.InputDiffs.Changed.Count);
        AssertED.AreEqual(currentInput, this._merger.ResultED.InputDiffs.Changed[0].Current);
        AssertED.AreEqual(updatedInput, this._merger.ResultED.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsTrue(this._merger.ResultED.CanMerge);
        Assert.AreEqual(1, this._merger.ResultED.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedInput.Settings[0]), this._merger.ResultED.MergeActions[0]);
    }

    [Test]
    public void Detects_Settings_Changed_Preserved()
    {
        // Arrange
        this.Detects_Settings_Changed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];
        currentInput.Settings[0].Preserve = true;

        // Act
        var canMerge = this.Act();

        // Arrange
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultED.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultED.InputDiffs.Changed.Count);
        AssertED.AreEqual(currentInput, this._merger.ResultED.InputDiffs.Changed[0].Current);
        AssertED.AreEqual(updatedInput, this._merger.ResultED.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultED.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultED.HasDifferences);
        Assert.IsFalse(this._merger.ResultED.CanMerge);
        Assert.IsFalse(this._merger.ResultED.MergeActions.Any());
    }

    [Test]
    public void Creates_Merge_Actions()
    {
        // Arrange
        var (addedInput, removedInput, currentChangingInput, updatedChangingInput, 
            addedSetting, removedSetting, updatedChangingSetting, 
            addedMapping, removedMapping, removedMappingButPreserved, currentChangedMapping, updatedChangedMapping, currentChangedMappingButPreserved, updatedChangedMappingButPreserved) = this.Creates_Merge_Actions_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);

        var mergeActionsIdx = -1;

        Assert.AreEqual(1, this._merger.ResultED.InputDiffs.Added.Count);
        AssertED.AreEqual(addedInput, this._merger.ResultED.InputDiffs.Added[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedInput), this._merger.ResultED.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.ResultED.InputDiffs.Removed.Count);
        AssertED.AreEqual(removedInput, this._merger.ResultED.InputDiffs.Removed[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedInput), this._merger.ResultED.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.ResultED.InputDiffs.Changed.Count);
        AssertED.AreEqual(currentChangingInput, this._merger.ResultED.InputDiffs.Changed[0].Current);
        AssertED.AreEqual(updatedChangingInput, this._merger.ResultED.InputDiffs.Changed[0].Updated);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedSetting), this._merger.ResultED.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedSetting), this._merger.ResultED.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangingSetting), this._merger.ResultED.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.ResultED.MappingDiffs.Added.Count);
        AssertED.AreEqual(addedMapping, this._merger.ResultED.MappingDiffs.Added[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedMapping), this._merger.ResultED.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.ResultED.MappingDiffs.Removed.Count);
        Assert2.DictionaryEquals(new[] { removedMapping, removedMappingButPreserved }.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), this._merger.ResultED.MappingDiffs.Removed.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), true, AssertED.AreEqual);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.ResultED.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.ResultED.MappingDiffs.Changed.Count);
        AssertED.AreEqual(currentChangedMapping, this._merger.ResultED.MappingDiffs.Changed[0].Current);
        AssertED.AreEqual(updatedChangedMapping, this._merger.ResultED.MappingDiffs.Changed[0].Updated);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangedMapping), this._merger.ResultED.MergeActions[++mergeActionsIdx]);
        AssertED.AreEqual(currentChangedMappingButPreserved, this._merger.ResultED.MappingDiffs.Changed[1].Current);
        AssertED.AreEqual(updatedChangedMappingButPreserved, this._merger.ResultED.MappingDiffs.Changed[1].Updated);

        Assert.AreEqual(mergeActionsIdx + 1, this._merger.ResultED.MergeActions.Count);
    }
    */
}