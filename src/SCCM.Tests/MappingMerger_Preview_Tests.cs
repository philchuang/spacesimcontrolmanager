using NUnit.Framework;
using SCCM.Core;

namespace SCCM.Tests;

[TestFixture]
public class MappingMerger_Preview_Tests
{
    private readonly MappingMerger _merger;

    private MappingData _current = new MappingData();
    private MappingData _updated = new MappingData();
    private bool _result;

    public MappingMerger_Preview_Tests()
    {
        this._merger = new MappingMerger();
    }

    private bool Act()
    {
        return this._result = this._merger.Preview(this._current, this._updated);
    }

    private static string RandomString() => Guid.NewGuid().ToString();

    private void Detects_All_Unchanged_Arrange()
    {
        this._current = new MappingData
        {
            Inputs = {
                new InputDevice { Type = "joystick", Instance = 1, Product = RandomString(), Settings = { 
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    }
                },
                new InputDevice { Type = "joystick", Instance = 2, Product = RandomString(), Settings = {
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { RandomString(), RandomString() } } },
                    }
                },
            },
            Mappings = {
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js1_{RandomString()}" },
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js1_{RandomString()}" },
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js2_{RandomString()}" },
                new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js2_{RandomString()}" },
            }
        };
        this._updated = new MappingData
        {
            Inputs = {
                this._current.Inputs[0].JsonCopy(),
                this._current.Inputs[1].JsonCopy(),
            },
            Mappings = {
                this._current.Mappings[0].JsonCopy(),
                this._current.Mappings[1].JsonCopy(),
                this._current.Mappings[2].JsonCopy(),
                this._current.Mappings[3].JsonCopy(),
            }
        };
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
        this._current = new MappingData
        {
            Inputs = { new InputDevice { Type = "joystick", Instance = 1, Product = RandomString() } }
        };
        this._updated = new MappingData
        {
            Inputs = { this._current.Inputs[0].JsonCopy(), new InputDevice { Type = "joystick", Instance = 2, Product = RandomString() } }
        };

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

    private void Create_2_Inputs_Arrange()
    {
        this._current = new MappingData
        {
            Inputs = {
                new InputDevice { Type = "joystick", Instance = 1, Product = Guid.NewGuid().ToString() },
                new InputDevice { Type = "joystick", Instance = 2, Product = Guid.NewGuid().ToString() },
            }
        };
        this._updated = new MappingData
        {
            Inputs = {
                this._current.Inputs[0].JsonCopy(),
                this._current.Inputs[1].JsonCopy(),
            }
        };
    }

    private void Detects_Inputs_Removed_NotPreserved_Arrange()
    {
        this.Create_2_Inputs_Arrange();

        this._current.Mappings.Add(new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = "js2_blah", Preserve = false });
        this._updated.Inputs.RemoveAt(1);
        // this wouldn't actually be present, but testing input removal in isolation
        this._updated.Mappings.Add(this._current.Mappings[0].JsonCopy());
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
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(null, MappingMergeActionMode.Remove, this._current.Inputs[1]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_Inputs_Removed_Preserved()
    {
        // Arrange
        this.Detects_Inputs_Removed_NotPreserved_Arrange();
        this._current.Mappings[0].Preserve = true;

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
        this.Create_2_Inputs_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];
        updatedInput.Settings.Add(new InputDeviceSetting { Name = RandomString(), Properties = { { "invert", "1" } } });

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

    private void Detects_InputSettings_Removed_NotPreserved_Arrange()
    {
        this.Create_2_Inputs_Arrange();
        this._current.Inputs[0].Settings.Add(new InputDeviceSetting { Name = RandomString(), Preserve = false, Properties = { { "invert", "1" } } });
        this._updated.Inputs[0].Settings.Clear();
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

    private void Detects_InputSettings_Changed_NotPreserved_Arrange()
    {
        this.Detects_All_Unchanged_Arrange();
        var currentInput = this._current.Inputs[0];
        var updatedInput = this._updated.Inputs[0];
        currentInput.Settings[0].Preserve = false;
        var updatedInputSettingProperties = this._updated.Inputs[0].Settings[0].Properties.First();
        updatedInput.Settings[0].Properties[updatedInputSettingProperties.Key] = RandomString();
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
        AssertSccm.AreEqual(new MappingMergeAction(currentInput.Settings[0], MappingMergeActionMode.Replace, updatedInput.Settings[0]), this._merger.Result.MergeActions[0]);
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
        this.Detects_All_Unchanged_Arrange();
        var addedMapping = new Mapping { ActionMap = RandomString(), Action = RandomString(), Input = $"js1_{RandomString()}", Preserve = true };
        this._updated.Mappings.Add(addedMapping);

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
        this.Detects_All_Unchanged_Arrange();
        var removedMapping = this._current.Mappings.Last();
        removedMapping.Preserve = false;
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
        this.Detects_All_Unchanged_Arrange();
        var originalMapping = this._current.Mappings.Last();
        originalMapping.Preserve = false;
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
        this.Detects_All_Unchanged_Arrange();
        var currentChangingInput = this._current.Inputs[0];
        var updatedChangingInput = this._updated.Inputs[0];
        // change input setting - asserted
        var currentChangingSetting = currentChangingInput.Settings[1];
        currentChangingSetting.Preserve = false;
        var updatedChangingSetting = updatedChangingInput.Settings[1];
        updatedChangingSetting.Properties = new Dictionary<string, string> { { RandomString(), RandomString() } };
        // add input setting - asserted
        var addedSetting = new InputDeviceSetting { Name = RandomString(), Preserve = true, Properties = { { RandomString(), RandomString() } } };
        updatedChangingInput.Settings.Add(addedSetting);
        // remove input setting - asserted
        var removedSetting = currentChangingInput.Settings[0];
        removedSetting.Preserve = false;
        updatedChangingInput.Settings.RemoveAt(0);

        var currentUnchangingInput = this._current.Inputs[1];
        var updatedUnchangingInput = this._updated.Inputs[1];
        // change input setting but preserved - can't assert
        var changedSettingButPreserved = updatedUnchangingInput.Settings[1];
        currentUnchangingInput.Settings[1].Preserve = true;
        changedSettingButPreserved.Properties = new Dictionary<string, string> { { RandomString(), RandomString() } };
        // remove input setting but preserved - can't assert
        var removedSettingButPreserved = currentUnchangingInput.Settings[0];
        removedSettingButPreserved.Preserve = true;
        updatedUnchangingInput.Settings.RemoveAt(0);

        // remove input - asserted
        var removedInput = new InputDevice { Type = "joystick", Instance = 3, Product = RandomString() };
        this._current.Inputs.Add(removedInput);

        // add input - asserted
        var addedInput = new InputDevice { Type = "joystick", Instance = 4, Product = RandomString() };
        this._updated.Inputs.Add(addedInput);
        
        // change mapping and changed mapping but preserved - asserted
        var currentChangedMapping = this._current.Mappings[2];
        var updatedChangedMapping = this._updated.Mappings[2];
        updatedChangedMapping.Input = $"js2_{RandomString()}";
        var currentChangedMappingButPreserved = this._current.Mappings[3];
        currentChangedMappingButPreserved.Preserve = true;
        var updatedChangedMappingButPreserved = this._updated.Mappings[3];
        updatedChangedMappingButPreserved.Input = $"js2_{RandomString()}";
        // remove mapping and remove mapping but preserved - asserted
        var removedMapping = this._current.Mappings[0];
        var removedMappingButPreserved = this._current.Mappings[1];
        removedMappingButPreserved.Preserve = true;
        this._updated.Mappings.RemoveAt(1);
        this._updated.Mappings.RemoveAt(0);
        // add mapping - asserted
        var addedMapping = new Mapping { ActionMap = RandomString(), Action = RandomString(), Preserve = true, Input = $"js1_{RandomString()}" };
        this._updated.Mappings.Add(addedMapping);

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
        AssertSccm.AreEqual(new MappingMergeAction (currentChangingSetting, MappingMergeActionMode.Replace, updatedChangingSetting), this._merger.Result.MergeActions[++mergeActionsIdx]);

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