using NUnit.Framework;
using SCCM.Core;

namespace SCCM.Tests;

[TestFixture]
public class DataWriter_Write_Tests
{
    private readonly DataWriter _writer;
    private MappingData? _data;

    private static string GetSamplesDir()
    {
        var working = System.IO.Directory.GetCurrentDirectory();
        return new System.IO.DirectoryInfo(System.IO.Path.Combine(working, "../../../../../samples")).FullName;
    }

    private static string GetSampleJsonPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(GetSamplesDir(), "mappings.json")).FullName;
    }

    public DataWriter_Write_Tests()
    {
        _writer = new DataWriter(GetSampleJsonPath());
    }

    [OneTimeSetUp]
    public async Task Init()
    {
        this._data = new MappingData
        {
            ReadTime = DateTime.UtcNow,
        };
    }

    [Test]
    public void Write_DoesSomething()
    {
    }
}
