using NUnit.Framework;
using SCCM.Core;
using static SCCM.Tests.Extensions;

namespace SCCM.Tests;

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
        var hasChanges = this.Act();

        // Assert
        Assert.IsFalse(hasChanges);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Added.Count);
        AssertSccm.AreEqual(this._updated.Inputs[1], this._merger.Result.InputDiffs.Added[0]);
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(null, MappingMergeActionMode.Add, this._updated.Inputs[1]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_Inputs_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_Inputs_Removed_NotPreserved_Arrange();

        // Act
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Removed.Count);
        AssertSccm.AreEqual(this._current.Inputs[1], this._merger.Result.InputDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Removed.Count);
        AssertSccm.AreEqual(this._current.Mappings[0], this._merger.Result.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(2, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(null, MappingMergeActionMode.Remove, this._current.Inputs[1]), this._merger.Result.MergeActions[0]);
        AssertSccm.AreEqual(new MappingMergeAction(null, MappingMergeActionMode.Remove, this._current.Mappings[0]), this._merger.Result.MergeActions[1]);
    }

    [Test]
    public void Detects_Inputs_Removed_Preserved()
    {
        // Arrange
        this.Detects_Inputs_Removed_NotPreserved_Arrange();
        this._current.Mappings[0].Preserve = true;
        this._updated.Mappings.Add(this._current.Mappings[0].JsonCopy());

        // Act
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Removed.Count);
        AssertSccm.AreEqual(this._current.Inputs[1], this._merger.Result.InputDiffs.Removed[0]);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(2, this._merger.Result.InputDiffs.Changed.Count);
        AssertSccm.AreEqual(this._current.Inputs[0], this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSccm.AreEqual(this._updated.Inputs[0], this._merger.Result.InputDiffs.Changed[0].Updated);
        AssertSccm.AreEqual(this._current.Inputs[1], this._merger.Result.InputDiffs.Changed[1].Current);
        AssertSccm.AreEqual(this._updated.Inputs[1], this._merger.Result.InputDiffs.Changed[1].Updated);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSccm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSccm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(currentInput, MappingMergeActionMode.Add, updatedInput.Settings[0]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_InputSettings_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_InputSettings_Removed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSccm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSccm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(currentInput, MappingMergeActionMode.Remove, currentInput.Settings[0]), this._merger.Result.MergeActions[0]);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSccm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSccm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
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
        var hasChanges = this.Act();

        // Arrange
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSccm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSccm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(currentInput, MappingMergeActionMode.Replace, updatedInput.Settings[0]), this._merger.Result.MergeActions[0]);
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
        var hasChanges = this.Act();

        // Arrange
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Changed.Count);
        AssertSccm.AreEqual(currentInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSccm.AreEqual(updatedInput, this._merger.Result.InputDiffs.Changed[0].Updated);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Added.Count);
        AssertSccm.AreEqual(addedMapping, this._merger.Result.MappingDiffs.Added[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Removed.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(null, MappingMergeActionMode.Add, addedMapping), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_Mapping_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_Mapping_Removed_NotPreserved_Arrange();
        var removedMapping = this._current.Mappings.Last();

        // Act
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Removed.Count);
        AssertSccm.AreEqual(removedMapping, this._merger.Result.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(null, MappingMergeActionMode.Remove, removedMapping), this._merger.Result.MergeActions[0]);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Removed.Count);
        AssertSccm.AreEqual(removedMapping, this._merger.Result.MappingDiffs.Removed[0]);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Changed.Count);
        AssertSccm.AreEqual(originalMapping, this._merger.Result.MappingDiffs.Changed[0].Current);
        AssertSccm.AreEqual(changedMapping, this._merger.Result.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(originalMapping, MappingMergeActionMode.Replace, changedMapping), this._merger.Result.MergeActions[0]);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);
        Assert.IsFalse(this._merger.Result.InputDiffs.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.Result.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Changed.Count);
        AssertSccm.AreEqual(originalMapping, this._merger.Result.MappingDiffs.Changed[0].Current);
        AssertSccm.AreEqual(changedMapping, this._merger.Result.MappingDiffs.Changed[0].Updated);
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
        var hasChanges = this.Act();

        // Assert
        Assert.IsTrue(hasChanges);

        var mergeActionsIdx = -1;

        Assert.AreEqual(1, this._merger.Result.InputDiffs.Added.Count);
        AssertSccm.AreEqual(addedInput, this._merger.Result.InputDiffs.Added[0]);
        AssertSccm.AreEqual(new MappingMergeAction (null, MappingMergeActionMode.Add, addedInput), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.Result.InputDiffs.Removed.Count);
        AssertSccm.AreEqual(removedInput, this._merger.Result.InputDiffs.Removed[0]);
        AssertSccm.AreEqual(new MappingMergeAction (null, MappingMergeActionMode.Remove, removedInput), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.Result.InputDiffs.Changed.Count);
        AssertSccm.AreEqual(currentChangingInput, this._merger.Result.InputDiffs.Changed[0].Current);
        AssertSccm.AreEqual(updatedChangingInput, this._merger.Result.InputDiffs.Changed[0].Updated);
        AssertSccm.AreEqual(new MappingMergeAction (currentChangingInput, MappingMergeActionMode.Add, addedSetting), this._merger.Result.MergeActions[++mergeActionsIdx]);
        AssertSccm.AreEqual(new MappingMergeAction (currentChangingInput, MappingMergeActionMode.Remove, removedSetting), this._merger.Result.MergeActions[++mergeActionsIdx]);
        AssertSccm.AreEqual(new MappingMergeAction (currentChangingInput, MappingMergeActionMode.Replace, updatedChangingSetting), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.Result.MappingDiffs.Added.Count);
        AssertSccm.AreEqual(addedMapping, this._merger.Result.MappingDiffs.Added[0]);
        AssertSccm.AreEqual(new MappingMergeAction (null, MappingMergeActionMode.Add, addedMapping), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.Result.MappingDiffs.Removed.Count);
        Assert2.DictionaryEquals(new[] { removedMapping, removedMappingButPreserved }.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), this._merger.Result.MappingDiffs.Removed.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), true, AssertSccm.AreEqual);
        AssertSccm.AreEqual(new MappingMergeAction (null, MappingMergeActionMode.Remove, removedMapping), this._merger.Result.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.Result.MappingDiffs.Changed.Count);
        AssertSccm.AreEqual(currentChangedMapping, this._merger.Result.MappingDiffs.Changed[0].Current);
        AssertSccm.AreEqual(updatedChangedMapping, this._merger.Result.MappingDiffs.Changed[0].Updated);
        AssertSccm.AreEqual(new MappingMergeAction (currentChangedMapping, MappingMergeActionMode.Replace, updatedChangedMapping), this._merger.Result.MergeActions[++mergeActionsIdx]);
        AssertSccm.AreEqual(currentChangedMappingButPreserved, this._merger.Result.MappingDiffs.Changed[1].Current);
        AssertSccm.AreEqual(updatedChangedMappingButPreserved, this._merger.Result.MappingDiffs.Changed[1].Updated);

        Assert.AreEqual(mergeActionsIdx + 1, this._merger.Result.MergeActions.Count);
    }
}