using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;
using static SSCM.Tests.Extensions;

namespace SSCM.Elite.Tests;

[TestFixture]
public class EDDataSerializer_Read_Tests : DataSerializer_Read_Tests<EDMappingData>
{
    protected override string SourceFilePath => string.Empty; // TODO

    public EDDataSerializer_Read_Tests()
    {
    }

    protected override EDMappingData CreateDataForRead()
    {
        // matches data from mappings.3.17.4.sample.json
        var expected = new EDMappingData
        {
            ReadTime = DateTime.Parse("2022-12-22T05:42:36.1532351Z").ToUniversalTime(),
            // TODO
        };
        return expected;
    }

    protected override void AssertAreEqual(EDMappingData? expected, EDMappingData? actual) => AssertED.AreEqual(expected, actual);
}
