using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using static SSCM.Tests.Extensions;

namespace SSCM.Elite.Tests;

[TestFixture]
public class EDDataSerializer_Read_Tests : DataSerializer_Read_Tests<EDMappingData>
{
    protected override string SourceFilePath => Path.Combine(Directory.GetCurrentDirectory(), "Data", "edmappings_1.json");

    public EDDataSerializer_Read_Tests()
    {
    }

    protected override EDMappingData CreateDataForRead() => EDDataSerializer_Write_Tests.CreateTestData();

    protected override void AssertAreEqual(EDMappingData? expected, EDMappingData? actual) => AssertED.AreEqual(expected, actual);

    [Test]
    public override Task Read_MatchesSampleData() => base.Read_MatchesSampleData();

    [Test]
    public override Task Handles_Malformed_MappingsFile() => base.Handles_Malformed_MappingsFile();


    [Test]
    public override Task Handles_NotFound_MappingsFile() => base.Handles_NotFound_MappingsFile();
}
