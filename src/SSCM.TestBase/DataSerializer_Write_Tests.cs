using Newtonsoft.Json;
using NUnit.Framework;
using SSCM.Core;

namespace SSCM.Tests;

[TestFixture]
public abstract class DataSerializer_Write_Tests<TData>
    where TData: class
{
    private readonly DataSerializer<TData> _serializer;
    private TData? _data;

    private string TestDataPath => new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TestContext.CurrentContext.Test.Name, "test.json")).FullName;

    protected DataSerializer_Write_Tests()
    {
        _serializer = new DataSerializer<TData>(this.TestDataPath) { Formatting = Formatting.None };
    }

    protected abstract TData CreateDataForWrite();
    protected abstract Task<string> GetExpectedJson();

    [OneTimeSetUp]
    public virtual async Task Init()
    {
        this._data = this.CreateDataForWrite();

        await this._serializer.Write(this._data);
    }

    public virtual async Task Write_MatchesSampleJson()
    {
        var expectedStrRead = await GetExpectedJson();
        var expectedData = JsonConvert.DeserializeObject<TData>(expectedStrRead);
        var expectedStrWrite = JsonConvert.SerializeObject(expectedData);
        var actualStrRead = await System.IO.File.ReadAllTextAsync(this._serializer.SavePath);

        Assert.AreEqual(expectedStrWrite, actualStrRead);
    }
}
