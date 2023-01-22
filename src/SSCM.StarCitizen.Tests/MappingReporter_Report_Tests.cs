using NUnit.Framework;
using SSCM.Core;
using SSCM.StarCitizen;
using SSCM.Tests;
using static SSCM.Tests.Extensions;

namespace SSCM.StarCitizen.Tests;

[TestFixture]
public class MappingReporter_Report_Tests
{
    protected readonly MappingReporter _reporter;

    public MappingReporter_Report_Tests()
    {
        this._reporter = new MappingReporter();
    }

    private const string EXPECTED_INPUT_HEADER = @"Id,Type,Name,Preserve,SettingNames";
    private const string EXPECTED_MAPPING_HEADER = @"Group,Action,Preserve,InputType,Binding,Options";

    [Test]
    public void Report_Outputs_Inputs()
    {
        // Arrange
        var expected = new List<string> { EXPECTED_INPUT_HEADER };
        var data = new SCMappingData();
        // basic case
        var input = new SCInputDevice {
            Type = RandomString(),
            Instance = 1,
            Product = RandomString(),
            Preserve = true,
        };
        data.Inputs.Add(input);
        expected.Add($"{input.Id},{input.Type},{input.Product},{input.Preserve},");
        // basic case with options
        input = new SCInputDevice {
            Type = RandomString(),
            Instance = 2,
            Product = RandomString(),
            Preserve = true,
            Settings = {
                new SCInputDeviceSetting { Name = "A" + RandomString(4) },
                new SCInputDeviceSetting { Name = "B" + RandomString(4) },
            }
        };
        data.Inputs.Add(input);
        expected.Add($"{input.Id},{input.Type},{input.Product},{input.Preserve},\"{string.Join(", ", input.Settings.Select(s => s.Name).OrderBy(s => s))}\"");

        // Act
        var actual = this._reporter.ReportInputs(data, preservedOnly: false);

        // Assert
        Assert2.EnumerableEquals(expected, actual.Split("\n").Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    [Test]
    public void Report_Outputs_Mappings()
    {
        // Arrange
        var expected = new List<string> { EXPECTED_MAPPING_HEADER };
        var data = new SCMappingData();
        // basic case
        var mapping = new SCMapping {
            ActionMap = RandomString(),
            Action = RandomString(),
            Preserve = true,
            InputType = RandomString(),
            Input = RandomString(),
        };
        data.Mappings.Add(mapping);
        expected.Add($"{mapping.ActionMap},{mapping.Action},{mapping.Preserve},{mapping.InputType},{mapping.Input},");
        // basic case with options
        mapping = new SCMapping {
            ActionMap = RandomString(),
            Action = RandomString(),
            Preserve = true,
            InputType = RandomString(),
            Input = RandomString(),
            MultiTap = 2,
        };
        data.Mappings.Add(mapping);
        expected.Add($"{mapping.ActionMap},{mapping.Action},{mapping.Preserve},{mapping.InputType},{mapping.Input},\"MultiTap: {mapping.MultiTap}\"");

        // Act
        var actual = this._reporter.ReportMappings(data, preservedOnly: false);

        // Assert
        Assert2.EnumerableEquals(expected, actual.Split("\n").Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    [Test]
    public void Report_Outputs_Inputs_and_Mappings_PreservedOnly()
    {
        // Arrange
        var expected = new List<string> { EXPECTED_INPUT_HEADER };
        var data = new SCMappingData();
        // basic case
        var input = new SCInputDevice {
            Type = RandomString(),
            Instance = 1,
            Product = RandomString(),
            Preserve = true,
        };
        data.Inputs.Add(input);
        expected.Add($"{input.Id},{input.Type},{input.Product},{input.Preserve},");
        // not preserved
        input = new SCInputDevice {
            Type = RandomString(),
            Instance = 2,
            Product = RandomString(),
            Preserve = false,
        };
        data.Inputs.Add(input);

        expected.Add(EXPECTED_MAPPING_HEADER);
        // basic case
        var mapping = new SCMapping {
            ActionMap = RandomString(),
            Action = RandomString(),
            Preserve = true,
            InputType = RandomString(),
            Input = RandomString(),
        };
        data.Mappings.Add(mapping);
        expected.Add($"{mapping.ActionMap},{mapping.Action},{mapping.Preserve},{mapping.InputType},{mapping.Input},");
        // not preserved
        mapping = new SCMapping {
            ActionMap = RandomString(),
            Action = RandomString(),
            Preserve = false,
            InputType = RandomString(),
            Input = RandomString(),
        };
        data.Mappings.Add(mapping);

        // Act
        var actual = this._reporter.Report(data, preservedOnly: true);

        // Assert
        Assert2.EnumerableEquals(expected, actual.Split("\n").Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)));    }
}