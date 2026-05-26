using NUnit.Framework;
using SSCM.Core;

namespace SSCM.Tests;

[TestFixture]
public class InteractiveChangeSession_Tests
{
    [Test]
    public void Toggle_Changes_Selection()
    {
        var session = CreateSession();

        session.Toggle("001");

        Assert.False(session.Rows.Single(r => r.RowId == "001").IsSelected);
    }

    [Test]
    public void SelectAll_Selects_All_Rows()
    {
        var session = CreateSession();
        session.ClearSelection();

        session.SelectAll();

        Assert.True(session.Rows.All(r => r.IsSelected));
    }

    [Test]
    public void ApplySelected_Applies_And_Removes_Selected_Rows()
    {
        var applied = new List<string>();
        var session = CreateSession(applied);

        var count = session.ApplySelected();

        Assert.AreEqual(1, count);
        Assert.AreEqual(new [] { "001" }, applied);
        Assert.AreEqual(new [] { "002" }, session.Rows.Select(r => r.RowId).ToArray());
    }

    [Test]
    public void ApplySelected_Does_Nothing_When_Nothing_Selected()
    {
        var applied = new List<string>();
        var session = CreateSession(applied);
        session.ClearSelection();

        var count = session.ApplySelected();

        Assert.AreEqual(0, count);
        Assert.IsEmpty(applied);
        Assert.AreEqual(2, session.Rows.Count);
    }

    private static InteractiveChangeSession CreateSession(IList<string>? applied = null)
    {
        applied ??= new List<string>();
        return new InteractiveChangeSession(new [] {
            new InteractiveChangeRow("001", "Update", "one", "a", "b", true, () => { applied.Add("001"); return true; }),
            new InteractiveChangeRow("002", "Update", "two", "c", "d", false, () => { applied.Add("002"); return true; }),
        });
    }
}
