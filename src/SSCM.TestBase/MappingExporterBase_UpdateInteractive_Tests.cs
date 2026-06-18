using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests.Mocks;

namespace SSCM.Tests;

[TestFixture]
public class MappingExporterBase_UpdateInteractive_Tests
{
    [Test]
    public async Task UpdateInteractive_Returns_False_And_Does_Not_Prompt_When_No_Rows()
    {
        // Arrange
        var exporter = new FakeInteractiveExporter([]);
        var userInput = new RecordingUserInput();

        // Act
        var actual = await exporter.UpdateInteractive("source", userInput);

        // Assert
        Assert.False(actual);
        Assert.IsEmpty(userInput.Messages);
        Assert.AreEqual(0, exporter.SaveInteractiveCount);
    }

    [Test]
    public void UpdateInteractive_Throws_When_User_Declines_Start()
    {
        // Arrange
        var exporter = new FakeInteractiveExporter([
            CreateRow("one"),
        ]);
        var userInput = new RecordingUserInput(false);

        // Act/Assert
        Assert.ThrowsAsync<UserInputCancelledException>(() => exporter.UpdateInteractive("source", userInput));
        Assert.AreEqual(0, exporter.SaveInteractiveCount);
        Assert.AreEqual("\nStart interactive export?", userInput.Messages.Single());
    }

    [Test]
    public async Task UpdateInteractive_Returns_False_And_Does_Not_Save_When_User_Rejects_Every_Row()
    {
        // Arrange
        var applied = new List<string>();
        var exporter = new FakeInteractiveExporter([
            CreateRow("one", applied),
            CreateRow("two", applied),
        ]);
        var userInput = new RecordingUserInput(true, false, false);

        // Act
        var actual = await exporter.UpdateInteractive("source", userInput);

        // Assert
        Assert.False(actual);
        Assert.IsEmpty(applied);
        Assert.AreEqual(0, exporter.SaveInteractiveCount);
        Assert.AreEqual(3, userInput.Messages.Count);
    }

    [Test]
    public void UpdateInteractive_Throws_And_Does_Not_Save_When_User_Declines_Finish()
    {
        // Arrange
        var applied = new List<string>();
        var exporter = new FakeInteractiveExporter([
            CreateRow("one", applied),
        ]);
        var userInput = new RecordingUserInput(true, true, false);

        // Act/Assert
        Assert.ThrowsAsync<UserInputCancelledException>(() => exporter.UpdateInteractive("source", userInput));
        Assert.AreEqual(new[] { "one" }, applied);
        Assert.AreEqual(0, exporter.SaveInteractiveCount);
        Assert.AreEqual("\nFinish interactive export and save changes?", userInput.Messages.Last());
    }

    [Test]
    public async Task UpdateInteractive_Saves_And_Returns_True_When_User_Applies_And_Confirms()
    {
        // Arrange
        var applied = new List<string>();
        var exporter = new FakeInteractiveExporter([
            CreateRow("one", applied),
        ]);
        var userInput = new RecordingUserInput(true, true, true);

        // Act
        var actual = await exporter.UpdateInteractive("source", userInput);

        // Assert
        Assert.True(actual);
        Assert.AreEqual(new[] { "one" }, applied);
        Assert.AreEqual(1, exporter.SaveInteractiveCount);
    }

    [Test]
    public async Task UpdateInteractive_Uses_None_Placeholders_For_Blank_Current_And_New_Values()
    {
        // Arrange
        var exporter = new FakeInteractiveExporter([
            new InteractiveChangeRow("one", "Update", "one", "", " ", true, () => false),
        ]);
        var userInput = new RecordingUserInput(true, false);

        // Act
        await exporter.UpdateInteractive("source", userInput);

        // Assert
        Assert.AreEqual("Update [one] <none> => <none> ?", userInput.Messages[1]);
    }

    private static InteractiveChangeRow CreateRow(string id, IList<string>? applied = null)
    {
        return new InteractiveChangeRow(id, "Update", id, "old", "new", true, () => {
            applied?.Add(id);
            return true;
        });
    }

    private class FakeInteractiveExporter : MappingExporterBase<string>
    {
        private readonly InteractiveChangeSession _session;

        public int SaveInteractiveCount { get; private set; }

        public FakeInteractiveExporter(IEnumerable<InteractiveChangeRow> rows)
            : base(new PlatformForTest())
        {
            this._session = new InteractiveChangeSession(rows);
        }

        public override string Backup() => "backup";

        public override string RestoreLatest() => "restore";

        public override Task<bool> Preview(string source) => Task.FromResult(false);

        public override Task<bool> Update(string source) => Task.FromResult(false);

        public override Task<InteractiveChangeSession> CreateInteractiveSession(string source) => Task.FromResult(this._session);

        public override Task SaveInteractive()
        {
            this.SaveInteractiveCount++;
            return Task.CompletedTask;
        }
    }

    private class RecordingUserInput : IUserInput
    {
        private readonly Queue<bool> _answers;

        public List<string> Messages { get; } = new();

        public RecordingUserInput(params bool[] answers)
        {
            this._answers = new Queue<bool>(answers);
        }

        public int MultipleChoice(string message, IList<string> choices)
        {
            throw new NotImplementedException();
        }

        public bool YesNo(string message, bool defaultAnswer = true)
        {
            this.Messages.Add(message);
            return this._answers.Count == 0 ? defaultAnswer : this._answers.Dequeue();
        }
    }
}
