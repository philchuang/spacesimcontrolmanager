using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using static SSCM.Tests.Extensions;

namespace SSCM.StarCitizen.Tests;

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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanMerge);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any());
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
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Added.Count);
        AssertSC.AreEqual(this._updated.Inputs[1], this._merger.ResultSC.InputDiffs.Added[0]);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, this._updated.Inputs[1]), this._merger.ResultSC.MergeActions[0]);
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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Removed.Count);
        AssertSC.AreEqual(this._current.Inputs[1], this._merger.ResultSC.InputDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Removed.Count);
        AssertSC.AreEqual(this._current.Mappings[0], this._merger.ResultSC.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(2, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, this._current.Inputs[1]), this._merger.ResultSC.MergeActions[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, this._current.Mappings[0]), this._merger.ResultSC.MergeActions[1]);
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
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Removed.Count);
        AssertSC.AreEqual(this._current.Inputs[1], this._merger.ResultSC.InputDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(0, this._merger.ResultSC.MergeActions.Count);
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
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(2, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(this._current.Inputs[0], this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(this._updated.Inputs[0], this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        AssertSC.AreEqual(this._current.Inputs[1], this._merger.ResultSC.InputDiffs.Changed[1].Current);
        AssertSC.AreEqual(this._updated.Inputs[1], this._merger.ResultSC.InputDiffs.Changed[1].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanMerge);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any());
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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, updatedInput.Settings[0]), this._merger.ResultSC.MergeActions[0]);
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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, currentInput.Settings[0]), this._merger.ResultSC.MergeActions[0]);
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
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanMerge);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any());
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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedInput.Settings[0]), this._merger.ResultSC.MergeActions[0]);
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
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanMerge);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any());
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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Added.Count);
        AssertSC.AreEqual(addedMapping, this._merger.ResultSC.MappingDiffs.Added[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedMapping), this._merger.ResultSC.MergeActions[0]);
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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Removed.Count);
        AssertSC.AreEqual(removedMapping, this._merger.ResultSC.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.ResultSC.MergeActions[0]);
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
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Removed.Count);
        AssertSC.AreEqual(removedMapping, this._merger.ResultSC.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanMerge);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any());
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
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Changed.Count);
        AssertSC.AreEqual(originalMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Current);
        AssertSC.AreEqual(changedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, changedMapping), this._merger.ResultSC.MergeActions[0]);
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
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Changed.Count);
        AssertSC.AreEqual(originalMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Current);
        AssertSC.AreEqual(changedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanMerge);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any());
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

        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Added.Count);
        AssertSC.AreEqual(addedInput, this._merger.ResultSC.InputDiffs.Added[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedInput), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Removed.Count);
        AssertSC.AreEqual(removedInput, this._merger.ResultSC.InputDiffs.Removed[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedInput), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentChangingInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedChangingInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedSetting), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedSetting), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangingSetting), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Added.Count);
        AssertSC.AreEqual(addedMapping, this._merger.ResultSC.MappingDiffs.Added[0]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedMapping), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.ResultSC.MappingDiffs.Removed.Count);
        Assert2.DictionaryEquals(new[] { removedMapping, removedMappingButPreserved }.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), this._merger.ResultSC.MappingDiffs.Removed.ToDictionary(m => $"{m.ActionMap}-{m.Action}"), true, AssertSC.AreEqual);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.ResultSC.MappingDiffs.Changed.Count);
        AssertSC.AreEqual(currentChangedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedChangedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Updated);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangedMapping), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSC.AreEqual(currentChangedMappingButPreserved, this._merger.ResultSC.MappingDiffs.Changed[1].Current);
        AssertSC.AreEqual(updatedChangedMappingButPreserved, this._merger.ResultSC.MappingDiffs.Changed[1].Updated);

        Assert.AreEqual(mergeActionsIdx + 1, this._merger.ResultSC.MergeActions.Count);
    }
}