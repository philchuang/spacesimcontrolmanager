using NUnit.Framework;
using SSCM.Core;

namespace SSCM.Tests;

public static class AssertSscm
{
    public static void AreEqual(MappingMergeAction? expected, MappingMergeAction? actual, Action<object, object>? asserter = null)
    {
        if (ReferenceEquals(expected, actual)) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Mode, actual.Mode, nameof(expected.Mode));
        Assert.AreEqual(expected.ExistingIsPreserved, actual.ExistingIsPreserved, nameof(expected.ExistingIsPreserved));
        if (asserter != null)
        {
            asserter(expected.Value, actual.Value);
        }
        else
        {
            Assert.AreSame(expected.Value, actual.Value, nameof(expected.Value));
        }
    }
}