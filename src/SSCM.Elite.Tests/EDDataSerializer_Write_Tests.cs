using Newtonsoft.Json;
using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;

namespace SSCM.Elite.Tests;

[TestFixture]
public class EDDataSerializer_Write_Tests : DataSerializer_Write_Tests<EDMappingData>
{
    public EDDataSerializer_Write_Tests()
    {
    }

    protected override EDMappingData CreateDataForWrite()
    {
        return new EDMappingData
        {
            ReadTime = DateTime.Parse("2022-12-22T05:42:36.1532351Z").ToUniversalTime(),
            // TODO
        };
    }

    protected override Task<string> GetExpectedJson() => Task.FromResult("TODO");

    [OneTimeSetUp]
    protected override Task Init() => base.Init();

    [Test]
    public override Task Write_MatchesSampleJson() => base.Write_MatchesSampleJson();
}
