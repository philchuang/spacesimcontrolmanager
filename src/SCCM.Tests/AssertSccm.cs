using NUnit.Framework;
using SCCM.Core;

namespace SCCM.Tests;

public static class AssertSccm
{
    public static void AreEqual(MappingData? expected, MappingData? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.ReadTime, actual.ReadTime, nameof(expected.ReadTime));
        Assert2.EnumerableEquals(expected.Inputs, actual.Inputs, AreEqual);
        Assert2.EnumerableEquals(expected.Mappings, actual.Mappings, AreEqual);
    }

    public static void AreEqual(InputDevice? expected, InputDevice? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Instance, actual.Instance, nameof(expected.Instance));
        Assert.AreEqual(expected.Product, actual.Product, nameof(expected.Product));
        Assert2.EnumerableEquals(expected.Settings, actual.Settings, AreEqual);
    }

    public static void AreEqual(InputDeviceSetting? expected, InputDeviceSetting? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Name, actual.Name, nameof(expected.Name));
        Assert.AreEqual(expected.Preserve, actual.Preserve, nameof(expected.Preserve));
        Assert2.DictionaryEquals(expected.Properties, actual.Properties, false, Assert.AreEqual);
    }

    public static void AreEqual(Mapping? expected, Mapping? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Action, actual.Action, nameof(expected.Action));
        Assert.AreEqual(expected.ActionMap, actual.ActionMap, nameof(expected.ActionMap));
        Assert.AreEqual(expected.Input, actual.Input, nameof(expected.Input));
        Assert.AreEqual(expected.MultiTap, actual.MultiTap, nameof(expected.MultiTap));
        Assert.AreEqual(expected.Preserve, actual.Preserve, nameof(expected.Preserve));
    }

    public static void AreEqual(MappingMergeAction? expected, MappingMergeAction? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Mode, actual.Mode, nameof(expected.Mode));
        Assert.AreSame(expected.NewValue, actual.NewValue, nameof(expected.NewValue));
        Assert.AreSame(expected.Target, actual.Target, nameof(expected.Target));
    }
}