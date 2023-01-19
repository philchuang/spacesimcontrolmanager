using NUnit.Framework;
using SCCM.Core;

namespace SCCM.Tests;

[TestFixture]
public class MappingMerger_Preview_Tests
{
    private readonly MappingMerger _merger;

    public MappingMerger_Preview_Tests()
    {
        this._merger = new MappingMerger();
    }

    [Test]
    public void Detects_Inputs_Unchanged()
    {
        var current = new MappingData
        {
            Inputs = {
                new InputDevice { Type = "joystick", Instance = 1, Product = Guid.NewGuid().ToString() },
                new InputDevice { Type = "joystick", Instance = 2, Product = Guid.NewGuid().ToString() },
            }
        };
        var updated = new MappingData
        {
            Inputs = {
                current.Inputs[0].JsonCopy(),
                current.Inputs[1].JsonCopy(),
            }
        };

        var hasChanges = this._merger.Preview(current, updated);

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
        var current = new MappingData
        {
            Inputs = { new InputDevice { Type = "joystick", Instance = 1, Product = Guid.NewGuid().ToString() } }
        };
        var updated = new MappingData
        {
            Inputs = { current.Inputs[0].JsonCopy(), new InputDevice { Type = "joystick", Instance = 2, Product = Guid.NewGuid().ToString() } }
        };

        var hasChanges = this._merger.Preview(current, updated);

        Assert.IsTrue(hasChanges);
        Assert.AreEqual(1, this._merger.Result.InputDiffs.Added.Count);
        AssertSccm.AreEqual(updated.Inputs[1], this._merger.Result.InputDiffs.Added[0]);
        Assert.IsFalse(this._merger.Result.MappingDiffs.Any());
        Assert.IsTrue(this._merger.Result.HasDifferences);
        Assert.IsTrue(this._merger.Result.CanMerge);
        Assert.AreEqual(1, this._merger.Result.MergeActions.Count);
        AssertSccm.AreEqual(new MappingMergeAction(null, MappingMergeActionMode.Add, updated.Inputs[1]), this._merger.Result.MergeActions[0]);
    }

    [Test]
    public void Detects_Inputs_Removed_NotPreserved()
    {
        // TODO current = 2 inputs
        // TODO updated = 1 inputs
        Assert.Fail();
    }

    [Test]
    public void Detects_Inputs_Removed_Preserved()
    {
        // TODO current = 2 inputs
        // TODO updated = 1 inputs
        Assert.Fail();
    }

    [Test]
    public void Detects_Inputs_InstanceId_Changed()
    {
        // TODO current = 2 inputs
        // TODO updated = 2 inputs
        Assert.Fail();
    }

    [Test]
    public void Detects_InputSettings_Added()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Detects_InputSettings_Removed_NotPreserved()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Detects_InputSettings_Removed_Preserved()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Detects_InputSettings_Changed_NotPreserved()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Detects_InputSettings_Changed_Preserved()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Detects_Mapping_Added()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Detects_Mapping_Removed_NotPreserved()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Detects_Mapping_Removed_Preserved()
    {
        // TODO implement
        Assert.Fail();
    }
    
    [Test]
    public void Detects_Mapping_Changed_NotPreserved()
    {
        // TODO implement
        Assert.Fail();
    }
    
    [Test]
    public void Detects_Mapping_Changed_Preserved()
    {
        // TODO implement
        Assert.Fail();
    }

    [Test]
    public void Creates_Merge_Actions()
    {
        // TODO implement combination of changes and preserved
        Assert.Fail();
    }
}