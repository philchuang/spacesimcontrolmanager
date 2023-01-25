using Newtonsoft.Json;
using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;

namespace SSCM.Elite.Tests;

[TestFixture]
public class EDDataSerializer_Write_Tests : DataSerializer_Write_Tests<EDMappingData>
{
    private const string KB = "Keyboard";
    private const string JOY1 = "231D0200";
    private const string JOY2 = "231D3205";

    public EDDataSerializer_Write_Tests()
    {
    }

    internal static EDMappingData CreateTestData()
    {
        return new EDMappingData
        {
            ReadTime = DateTime.Parse("2022-12-22T05:42:36.1532351Z").ToUniversalTime(),
            Mappings = {
                // primary + settings
                new EDMapping { Group = "Ship-FlightRotation", Name = "PitchAxisRaw", Primary = new EDBinding(new EDBindingKey(JOY1, "Joy_YAxis")), Settings = { 
                    new EDMappingSetting("Ship-FlightRotation-PitchAxisRaw", "Deadzone", "0.00000000"),
                    new EDMappingSetting("Ship-FlightRotation-PitchAxisRaw", "Inverted", "1"),
                    }
                },
                // primary + secondary
                new EDMapping { Group = "Ship-FlightThrottle", Name = "BackwardKey", Primary = new EDBinding(new EDBindingKey(KB, "Key_S")), Secondary = new EDBinding(new EDBindingKey(JOY2, "Joy_POV1Down")) },
                // secondary
                new EDMapping { Group = "Ship-FlightThrottle", Name = "SetSpeed75", Secondary = new EDBinding(new EDBindingKey(JOY2, "Joy_POV1Right")) },
            },
            Settings = {
                new EDMappingSetting { Group = "Ship-FullSpectrumSystemScanner", Name = "FSSMouseLinearity", Value = "1.00000000", Preserve = true }
            }
        };
    }

    protected override EDMappingData CreateDataForWrite() => CreateTestData();

    protected override Task<string> GetExpectedJson() => File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "Data", "edmappings_1.json"));

    [Test]
    public override Task Write_MatchesSampleJson() => base.Write_MatchesSampleJson();
}
