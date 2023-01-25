using NUnit.Framework;
using SSCM.Core;
using static SSCM.Tests.Extensions;

namespace SSCM.Tests;

[TestFixture]
public abstract class DataSerializer_Read_Tests<TData>
    where TData: class
{
    private DataSerializer<TData> _serializer = new DataSerializer<TData>(string.Empty);

    private string TestDataPath => new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TestContext.CurrentContext.Test.Name, "test.json")).FullName;

    protected abstract string SourceFilePath { get; }

    protected DataSerializer_Read_Tests()
    {
    }

    [SetUp]
    public void Init()
    {
        System.IO.Directory.CreateDirectory(new FileInfo(this.TestDataPath).DirectoryName);
        System.IO.File.Copy(this.SourceFilePath, this.TestDataPath, true);
        this._serializer = new DataSerializer<TData>(this.TestDataPath);
    }

    [TearDown]
    protected void Cleanup()
    {
        System.IO.Directory.Delete(new FileInfo(this.TestDataPath).DirectoryName, true);
    }

    protected abstract TData CreateDataForRead();

    protected abstract void AssertAreEqual(TData? expected, TData? actual);

    [Test]
    public async Task Read_MatchesSampleData()
    {
        // matches data from mappings.3.17.4.sample.json
        var expected = this.CreateDataForRead();
        var actual = await this._serializer.Read();

        AssertAreEqual(expected, actual);
    }

    [Test]
    public async Task Handles_Malformed_MappingsFile()
    {
        // Arrange
        await System.IO.File.WriteAllTextAsync(this.TestDataPath, RandomString());

        // Act
        var ex = Assert.ThrowsAsync<SscmException>(() => this._serializer.Read());
        Assert.IsTrue(ex.Message.StartsWith("Could not read SSCM mapping data file at"));
    }

    [Test]
    public async Task Handles_NotFound_MappingsFile()
    {
        // Arrange
        System.IO.File.Delete(this.TestDataPath);

        // Act
        var result = await this._serializer.Read();

        // Assert
        Assert.IsNull(result);
    }
}
