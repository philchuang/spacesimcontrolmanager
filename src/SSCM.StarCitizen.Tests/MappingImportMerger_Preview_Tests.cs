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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any());
    }

    [Test]
    public void Detects_Inputs_Added()
    {
        // Arrange
        this.Detects_Inputs_Added_Arrange();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Added.Count);
        AssertSC.AreEqual(this._updated.Inputs[1], this._merger.ResultSC.InputDiffs.Added[0]);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, this._updated.Inputs[1]), this._merger.ResultSC.MergeActions[0]);
    }

    [Test]
    public void Detects_Inputs_Removed_NotPreserved()
    {
        // Arrange
        this.Detects_Inputs_Removed_NotPreserved_Arrange();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Removed.Count);
        AssertSC.AreEqual(this._current.Inputs[1], this._merger.ResultSC.InputDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Removed.Count);
        AssertSC.AreEqual(this._current.Mappings[0], this._merger.ResultSC.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Removed.Count);
        AssertSC.AreEqual(this._current.Inputs[1], this._merger.ResultSC.InputDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Changed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(2, this._merger.ResultSC.MergeActions.Count);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count(m => !m.ExistingIsPreserved));
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count(m => m.ExistingIsPreserved));
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
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
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any(m => !m.ExistingIsPreserved));
    }

    [Test]
    public void Detects_InputSettings_Changed_NotPreserved()
    {
        // Arrange
        this.Detects_InputSettings_Changed_NotPreserved_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];

        // Act
        var canAutoMerge = this.Act();

        // Arrange
        Assert.IsTrue(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Arrange
        Assert.IsFalse(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any(m => !m.ExistingIsPreserved));
    }

    [Test]
    public void Detects_Mapping_Added()
    {
        // Arrange
        this.Detects_Mapping_Added_Arrange();
        var addedMapping = this._updated.Mappings.Last();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Added.Count);
        AssertSC.AreEqual(addedMapping, this._merger.ResultSC.MappingDiffs.Added[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Removed.Count);
        AssertSC.AreEqual(removedMapping, this._merger.ResultSC.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Removed.Count);
        AssertSC.AreEqual(removedMapping, this._merger.ResultSC.MappingDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Changed.Any());
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any(m => !m.ExistingIsPreserved));
    }
    
    [Test]
    public void Detects_Mapping_Changed_NotPreserved()
    {
        // Arrange
        this.Detects_Mapping_Changed_NotPreserved_Arrange();
        var originalMapping = this._current.Mappings.Last();
        var changedMapping = this._updated.Mappings.Last();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Changed.Count);
        AssertSC.AreEqual(originalMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Current);
        AssertSC.AreEqual(changedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
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
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Changed.Count);
        AssertSC.AreEqual(originalMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Current);
        AssertSC.AreEqual(changedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any(m => !m.ExistingIsPreserved));
    }

    [Test]
    public void Detects_Attribute_Added()
    {
        // Arrange
        var addedAttribute = base.Detects_Attribute_Added_Arrange();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Removed.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Changed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.AttributeDiffs.Added.Count);
        AssertSC.AreEqual(addedAttribute, this._merger.ResultSC.AttributeDiffs.Added[0]);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedAttribute), this._merger.ResultSC.MergeActions[0], (c, u) => AssertSC.AreEqual((SCAttribute) c, (SCAttribute) u));
    }

    [Test]
    public void Detects_Attribute_Removed_NotPreserved()
    {
        // Arrange
        var removedAttribute = base.Detects_Attribute_Removed_NotPreserved_Arrange();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Changed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.AttributeDiffs.Removed.Count);
        AssertSC.AreEqual(removedAttribute, this._merger.ResultSC.AttributeDiffs.Removed[0]);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedAttribute), this._merger.ResultSC.MergeActions[0], (c, u) => AssertSC.AreEqual((SCAttribute) c, (SCAttribute) u));
    }

    [Test]
    public void Detects_Attribute_Removed_Preserved()
    {
        // Arrange
        var removedAttribute = base.Detects_Attribute_Removed_NotPreserved_Arrange(true);

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Changed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.AttributeDiffs.Removed.Count);
        AssertSC.AreEqual(removedAttribute, this._merger.ResultSC.AttributeDiffs.Removed[0]);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any(m => !m.ExistingIsPreserved));
    }

    [Test]
    public void Detects_Attribute_Changed_NotPreserved()
    {
        // Arrange
        var (currentAttribute, updatedAttribute) = base.Detects_Attribute_Changed_NotPreserved_Arrange();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsTrue(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.AttributeDiffs.Changed.Count);
        AssertSC.AreEqual(currentAttribute, this._merger.ResultSC.AttributeDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedAttribute, this._merger.ResultSC.AttributeDiffs.Changed[0].Updated);
        Assert.IsTrue(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedAttribute), this._merger.ResultSC.MergeActions[0], (c, u) => AssertSC.AreEqual((SCAttribute) c, (SCAttribute) u));
    }

    [Test]
    public void Detects_Attribute_Changed_Preserved()
    {
        // Arrange
        var (currentAttribute, updatedAttribute) = base.Detects_Attribute_Changed_NotPreserved_Arrange(true);

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);
        Assert.IsTrue(this._merger.ResultSC.HasDifferences);
        Assert.IsFalse(this._merger.ResultSC.InputDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.MappingDiffs.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Added.Any());
        Assert.IsFalse(this._merger.ResultSC.AttributeDiffs.Removed.Any());
        Assert.AreEqual(1, this._merger.ResultSC.AttributeDiffs.Changed.Count);
        AssertSC.AreEqual(currentAttribute, this._merger.ResultSC.AttributeDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedAttribute, this._merger.ResultSC.AttributeDiffs.Changed[0].Updated);
        Assert.IsFalse(this._merger.ResultSC.CanAutoMerge);
        Assert.AreEqual(1, this._merger.ResultSC.MergeActions.Count);
        Assert.IsFalse(this._merger.ResultSC.MergeActions.Any(m => !m.ExistingIsPreserved));
    }

    [Test]
    public void Creates_Merge_Actions()
    {
        // TODO add attributes
        // Arrange
        var (addedInput, removedInput, currentChangingInput, updatedChangingInput, 
            addedSetting, removedSetting, removedSettingButPreserved, updatedChangingSetting, changedSettingButPreserved,
            addedMapping, removedMapping, removedMappingButPreserved, currentChangedMapping, updatedChangedMapping, currentChangedMappingButPreserved, updatedChangedMappingButPreserved) = this.Creates_Merge_Actions_Arrange();

        // Act
        var canAutoMerge = this.Act();

        // Assert
        Assert.IsFalse(canAutoMerge);

        var mergeActionsIdx = -1;

        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Added.Count);
        AssertSC.AreEqual(addedInput, this._merger.ResultSC.InputDiffs.Added[0]);
        Assert.AreEqual(1, this._merger.ResultSC.InputDiffs.Removed.Count);
        AssertSC.AreEqual(removedInput, this._merger.ResultSC.InputDiffs.Removed[0]);

        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedInput), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedInput), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(2, this._merger.ResultSC.InputDiffs.Changed.Count);
        AssertSC.AreEqual(currentChangingInput, this._merger.ResultSC.InputDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedChangingInput, this._merger.ResultSC.InputDiffs.Changed[0].Updated);
        
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedSetting), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedSetting), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangingSetting), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedSettingButPreserved, true), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, changedSettingButPreserved, true), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);

        Assert.AreEqual(1, this._merger.ResultSC.MappingDiffs.Added.Count);
        AssertSC.AreEqual(addedMapping, this._merger.ResultSC.MappingDiffs.Added[0]);

        Assert.AreEqual(2, this._merger.ResultSC.MappingDiffs.Removed.Count);
        Assert2.DictionaryEquals(new[] { removedMapping, removedMappingButPreserved }.ToDictionary(m => m.Id), this._merger.ResultSC.MappingDiffs.Removed.ToDictionary(m => m.Id), true, AssertSC.AreEqual);

        Assert.AreEqual(2, this._merger.ResultSC.MappingDiffs.Changed.Count);
        AssertSC.AreEqual(currentChangedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Current);
        AssertSC.AreEqual(updatedChangedMapping, this._merger.ResultSC.MappingDiffs.Changed[0].Updated);
        AssertSC.AreEqual(currentChangedMappingButPreserved, this._merger.ResultSC.MappingDiffs.Changed[1].Current);
        AssertSC.AreEqual(updatedChangedMappingButPreserved, this._merger.ResultSC.MappingDiffs.Changed[1].Updated);

        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Add, addedMapping), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMapping), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Remove, removedMappingButPreserved, true), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangedMapping), this._merger.ResultSC.MergeActions[++mergeActionsIdx]);
        AssertSscm.AreEqual(new MappingMergeAction(MappingMergeActionMode.Replace, updatedChangedMappingButPreserved, true), this._merger.ResultSC.MergeActions[++mergeActionsIdx], (e, a) => AssertSC.AreEqual((SCMapping) e, (SCMapping) a));

        Assert.AreEqual(mergeActionsIdx + 1, this._merger.ResultSC.MergeActions.Count);
    }
}