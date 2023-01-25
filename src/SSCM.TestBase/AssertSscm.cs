using NUnit.Framework;
using SSCM.Core;

namespace SSCM.Tests;

public static class AssertSscm
{
    public static void AreEqual(MappingMergeAction? expected, MappingMergeAction? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Mode, actual.Mode, nameof(expected.Mode));
        Assert.AreSame(expected.Value, actual.Value, nameof(expected.Value));
    }
}