using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;

namespace SSCM.Elite.Tests;

public static class AssertED
{
    public static void AreEqual(EDMappingData? expected, EDMappingData? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.ReadTime, actual.ReadTime, nameof(expected.ReadTime));
        Assert2.EnumerableEquals(expected.Mappings, actual.Mappings, AreEqual);
        Assert2.EnumerableEquals(expected.Settings, actual.Settings, AreEqual);
    }

    public static void AreEqual(EDMappingSetting? expected, EDMappingSetting? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Group, actual.Group, nameof(expected.Group));
        Assert.AreEqual(expected.Name, actual.Name, nameof(expected.Name));
        Assert.AreEqual(expected.Preserve, actual.Preserve, nameof(expected.Preserve));
        Assert.AreEqual(expected.Value, actual.Value, nameof(expected.Value));
    }

    public static void AreEqual(EDMapping? expected, EDMapping? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Group, actual.Group, nameof(expected.Group));
        Assert.AreEqual(expected.Name, actual.Name, nameof(expected.Name));
        AreEqual(expected.Binding, actual.Binding);
        AreEqual(expected.Primary, actual.Primary);
        AreEqual(expected.Secondary, actual.Secondary);
        Assert2.EnumerableEquals(expected.Settings, actual.Settings, AreEqual);
    }

    public static void AreEqual(EDBinding? expected, EDBinding? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Preserve, actual.Preserve, nameof(expected.Preserve));
        AreEqual(expected.Key, actual.Key);
        Assert2.EnumerableEquals(expected.Modifiers, actual.Modifiers, AreEqual);
    }

    public static void AreEqual(EDBindingKey? expected, EDBindingKey? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Device, actual.Device, nameof(expected.Device));
        Assert.AreEqual(expected.Key, actual.Key, nameof(expected.Key));
    }
}