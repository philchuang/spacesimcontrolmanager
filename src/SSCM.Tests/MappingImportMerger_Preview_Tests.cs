using NUnit.Framework;
using SSCM.Core;
using static SSCM.Tests.Extensions;

namespace SSCM.Tests;

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
        this.Create_2_Inputs_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsFalse(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.CanMerge);
        Assert.IsFalse(this._merger.Result.MergeActions.Any());
    }

    [Test]
    public void Detects_Inputs_Added()
    {
        // Arrange
        this.Detects_Inputs_Added_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Added.Count);
        AssertSscm.AreEqual(this._updated.Inputs[1], this._merger.Result.InputDiffs.Added[0]);
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, this._updated.Inputs[1]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_Inputs_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_Inputs_Removed_NotPreserved_Arrange();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Removed.Count);
        AssertSscm.AreEqual(this._current.Inputs[1], this._merger.Result.InputDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Removed.Count);
        AssertSscm.AreEqual(this._current.Mappings[0], this._merger.Result.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(2, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, this._current.Inputs[1]), this._merger.Result.MergeActions[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, this._current.Mappings[0]), this._merger.Result.MergeActions[1]);
    }

    [Test]
    public void Detects_Inputs_Removed_Preserved()
    {
        // Arrange
        this.Detects_Inputs_Removed_NotPreserved_Arrange();
        this._current.Mappings[0].Preserve = true;
        this._updated.Mappings.Add(this._current.Mappings[0].JsonCopy());

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Removed.Count);
        AssertSscm.AreEqual(this._current.Inputs[1], this._merger.Result.InputDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.CanMerge);
        Assert.AreEqual(0, this._merger.Result.MergeActions.Count);
    }

    [Test]
    public void Detects_Inputs_InstanceId_Changed()
    {
        // Arrange
        this.Create_2_Inputs_Arrange();
        // swap the instance IDs
        this._updated.Inputs[0].Instance = 2;
        this._updated.Inputs[1].Instance = 1;

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(2, this._merger.Result.InputDiffs.Changed.Count);
        AssertSscm.AreEqual(this._current.Inputs[0], this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSscm.AreEqual(this._updated.Inputs[0], this._merger.Result.InputDiffs.Changed[0].Updated);
        AssertSscm.AreEqual(this._current.Inputs[1], this._merger.Result.InputDiffs.Changed[1].Current);
        AssertSscm.AreEqual(this._updated.Inputs[1], this._merger.Result.InputDiffs.Changed[1].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.CanMerge);
        Assert.IsFalse(this._merger.Result.MergeActions.Any());
    }

    [Test]
    public void Detects_InputSettings_Added()
    {
        // Arrange
        this.Detects_InputSettings_Added_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSscm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSscm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, updatedInput.Settings[0]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_InputSettings_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_InputSettings_Removed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSscm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSscm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, currentInput.Settings[0]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_InputSettings_Removed_Preserved()
    {
        // Arrange
        this.Detects_InputSettings_Removed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];
        currentInput.Settings[0].Preserve = true;

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSscm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSscm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.CanMerge);
        Assert.IsFalse(this._merger.Result.MergeActions.Any());
    }

    [Test]
    public void Detects_InputSettings_Changed_NotPreserved()
    {
        // Arrange
        this.Detects_InputSettings_Changed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var canMerge = this.Act();

        // Arrange
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSscm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSscm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedInput.Settings[0]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_InputSettings_Changed_Preserved()
    {
        // Arrange
        this.Detects_InputSettings_Changed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];
        currentInput.Settings[0].Preserve = true;

        // Act
        var canMerge = this.Act();

        // Arrange
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSscm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSscm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.CanMerge);
        Assert.IsFalse(this._merger.Result.MergeActions.Any());
    }

    [Test]
    public void Detects_Mapping_Added()
    {
        // Arrange
        this.Detects_Mapping_Added_Arrange();
        var addedMapping = this._updated.Mappings.Last();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Added.Count);
        AssertSscm.AreEqual(addedMapping, this._merger.Result.MappingDiffs.Added[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Removed.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedMapping), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_Mapping_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_Mapping_Removed_NotPreserved_Arrange();
        var removedMapping = this._current.Mappings.Last();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Removed.Count);
        AssertSscm.AreEqual(removedMapping, this._merger.Result.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_Mapping_Removed_Preserved()
    {
        // Arrange
        this.Detects_All_Unchanged_Arrange();
        var removedMapping = this._current.Mappings.Last();
        removedMapping.Preserve = true;
        this._updated.Mappings.RemoveAt(this._updated.Mappings.Count - 1);

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Removed.Count);
        AssertSscm.AreEqual(removedMapping, this._merger.Result.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.CanMerge);
        Assert.IsFalse(this._merger.Result.MergeActions.Any());
    }
    
    [Test]
    public void Detects_Mapping_Changed_NotPreserved()
    {
        // Arrange
        this.Detects_Mapping_Changed_NotPreserved_Arrange();
        var originalMapping = this._current.Mappings.Last();
        var changedMapping = this._updated.Mappings.Last();

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsTrue(canMerge);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Changed.Count);
        AssertSscm.AreEqual(originalMapping, this._merger.Result.MappingDiffs.Changed[0].Current);
        AssertSscm.AreEqual(changedMapping, this._merger.Result.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, changedMapping), this._merger.Result.MergeActions[0]);
    }
    
    [Test]
    public void Detects_Mapping_Changed_Preserved()
    {
        // Arrange
        this.Detects_All_Unchanged_Arrange();
        var originalMapping = this._current.Mappings.Last();
        originalMapping.Preserve = true;
        var changedMapping = this._updated.Mappings.Last();
        this._updated.Mappings.Last().Input = $"js1_{RandomString()}";

        // Act
        var canMerge = this.Act();

        // Assert
        Assert.IsFalse(canMerge);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Changed.Count);
        AssertSscm.AreEqual(originalMapping, this._merger.Result.MappingDiffs.Changed[0].Current);
        AssertSscm.AreEqual(changedMapping, this._merger.Result.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsFalse(this._merger.Result.CanMerge);
        Assert.IsFalse(this._merger.Result.MergeActions.Any());
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

        Assert.AreEqual(1, this._merger.Result.InputDiffs.Added.Count);
        AssertSscm.AreEqual(addedInput, this._merger.Result.InputDiffs.Added[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedInput), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.Result.InputDiffs.Removed.Count);
        AssertSscm.AreEqual(removedInput, this._merger.Result.InputDiffs.Removed[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedInput), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.Result.InputDiffs.Changed.Count);
        AssertSscm.AreEqual(currentChangingInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSscm.AreEqual(updatedChangingInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedSetting), this._merger.Result.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedSetting), this._merger.Result.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangingSetting), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Added.Count);
        AssertSscm.AreEqual(addedMapping, this._merger.Result.MappingDiffs.Added[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedMapping), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.Result.MappingDiffs.Removed.Count);
        Assert2.DictionaryEquals(new[] { removedMapping, removedMappingButPreserved }.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), this._merger.Result.MappingDiffs.Removed.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), true, AssertSscm.AreEqual);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.Result.MappingDiffs.Changed.Count);
        AssertSscm.AreEqual(currentChangedMapping, this._merger.Result.MappingDiffs.Changed[0].Current);
        AssertSscm.AreEqual(updatedChangedMapping, this._merger.Result.MappingDiffs.Changed[0].Updated);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangedMapping), this._merger.Result.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(currentChangedMappingButPreserved, this._merger.Result.MappingDiffs.Changed[1].Current);
        AssertSscm.AreEqual(updatedChangedMappingButPreserved, this._merger.Result.MappingDiffs.Changed[1].Updated);

        Assert.AreEqual(mergeActionsIdx + 1, this._merger.Result.MergeActions.Count);
    }
}