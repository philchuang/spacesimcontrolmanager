using NUnit.Framework;
using SSCM.Core;
using SSCM.Elite.Tests.Mocks;
using SSCM.Tests;
using SSCM.Tests.Mocks;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using static SSCM.Tests.Extensions;

namespace SSCM.Elite.Tests;

#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8619

[TestFixture]
public class MappingExporter_Update_Tests : TestBase
{
    private MappingExporter? _exporter;
    private readonly IPlatform _platform;
    private readonly EDFoldersForTest _folders;
    private EDMappingData _source = new EDMappingData();
    private XDocument? _inputXml = null;
    private XDocument? _outputXml = null;

    public MappingExporter_Update_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._folders = new EDFoldersForTest();
    }

    private string GameConfigPath => new FileInfo(Path.Combine(base.TestTempDir, "custom.4.0.binds")).FullName;

    [SetUp]
    protected async Task Init()
    {
        this._folders.GameConfigPath = this.GameConfigPath;
        this._exporter = new MappingExporter(this._platform, this._folders);
        this._exporter.StandardOutput += s => TestContext.Out.WriteLine($"[STD  ]\t{s}");
        this._exporter.DebugOutput    += s => TestContext.Out.WriteLine($"[DEBUG]\t{s}");
        this._exporter.WarningOutput  += s => TestContext.Out.WriteLine($"[WARN ]\t{s}");

        this._inputXml = await this.LoadXml(new FileInfo(Path.Combine(base.TestDataDir, "custom40binds_0.xml")).FullName);
    }

    private async Task<bool> Act()
    {
        // write _inputXml to GameConfigPath
        await this._inputXml.WriteToAsync(this.GameConfigPath);

        // execute export
        var changed = await this._exporter.Update(this._source);

        // read _outputXml
        this._outputXml = await this.LoadXml(this.GameConfigPath);

        return changed;
    }

    private void AssertBasics()
    {
        Assert.NotNull(this._outputXml, nameof(this._outputXml));
        Assert.NotNull(this._outputXml.Root, nameof(this._outputXml.Root));
        Assert.AreEqual("Root", this._outputXml.Root.Name.LocalName);
    }

    private XElement? GetElementForMapping(XDocument xd, EDMapping mapping)
    {
        return xd.XPathSelectElements($"/Root/{mapping.Name}").SingleOrDefault();
    }

    private XElement? GetElementForBinding(XDocument xd, EDMapping mapping, string type)
    {
        return xd.XPathSelectElements($"/Root/{mapping.Name}/{type}").SingleOrDefault();
    }

    private void RandomizeBindingValue(XElement binding)
    {
        binding.SetAttributeValue("Device", RandomString());
        binding.SetAttributeValue("Key", RandomString());
        binding.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(RandomizeBindingValue);
    }

    private string GetBindingValue(XElement binding)
    {
        return string.Join(
            " + ", 
            (new[] { binding }.Concat(binding.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
                .Select(GetBindingKeyValue))
        );
    }

    private string GetBindingKeyValue(XElement bindingElement) => $"{bindingElement!.GetAttribute("Device")}-{bindingElement!.GetAttribute("Key")}";

    private void Arrange_Default_MappingData(bool preserve)
    {
        this._source = new EDMappingData {
            Settings = {
            },
            Mappings = {
                new EDMapping("Ship-Cooling", "ToggleButtonUpInput") {
                    Primary = new EDBinding("Keyboard", "Key_V"), 
                    Secondary = new EDBinding("231D3205", "Joy_15", new[] { new EDBindingKey("231D3205", "Joy_1"), new EDBindingKey("231D3205", "Joy_2") }),
                    Settings = {
                        new EDMappingSetting("Ship-Cooling-ToggleButtonUpInput", "ToggleOn", "1"),
                    }
                },
                new EDMapping("Ship-Weapons", "CycleFireGroupPrevious") { 
                    Primary = EDBinding.UNBOUND(), 
                    Secondary = new EDBinding("231D0200", "Joy_22") },
                new EDMapping("Ship-Throttle", "ForwardKey") { 
                    Primary = new EDBinding("Keyboard", "Key_W"), 
                    Secondary = new EDBinding("231D3205", "Joy_POV1Up") },
                new EDMapping("Ship-FlightRotation", "PitchAxisRaw") { 
                    Binding = new EDBinding("231D0200", "Joy_YAxis"), 
                    Settings = { 
                        new EDMappingSetting("Ship-FlightRotation-PitchAxisRaw", "Deadzone", "0.00000000"), 
                        new EDMappingSetting("Ship-FlightRotation-PitchAxisRaw", "Inverted", "1") 
                    }
                },
            }
        };

        this._source.Mappings.SelectMany(m => new[] { m.Binding, m.Primary, m.Secondary }).ToList().ForEach(m => { if (m != null) m.Preserve = preserve; });
        this._source.Settings.Concat(this._source.Mappings.SelectMany(m => m.Settings)).ToList().ForEach(s => { s.Preserve = preserve; });
    }

    [Test]
    public async Task Update_overwrites_binding()
    {
        // NOTE also tests overwrite of Binding + settings

        // Arrange
        // use default mapping data
        this.Arrange_Default_MappingData(false);
        // preserve the binding & setting
        var mapping = this._source.Mappings.Single(m => m.Id == "Ship-FlightRotation-PitchAxisRaw");
        mapping.Binding.Preserve = true;
        mapping.Settings[0].Preserve = true;
        // get the binding in the xml and make sure it's different
        var bindingElement = this.GetElementForBinding(this._inputXml, mapping, nameof(mapping.Binding));
        RandomizeBindingValue(bindingElement);
        var inputBindingValue = GetBindingValue(bindingElement);
        // get the setting in the xml and make sure it's different
        var settingElement = bindingElement.Parent.Elements().First(e => e.Name.LocalName == "Inverted");
        settingElement.SetAttributeValue("Value", RandomString());
        var inputSettingValue = settingElement.GetAttribute("Value");

        // Act
        var changed = await this.Act();

        // Assert
        Assert.IsTrue(changed, nameof(changed));
        this.AssertBasics();

        var outputBindingElement = this.GetElementForBinding(this._outputXml, mapping, "Binding");
        Assert.NotNull(outputBindingElement, nameof(outputBindingElement));
        Assert.AreNotEqual(inputBindingValue, GetBindingValue(outputBindingElement));
        Assert.AreEqual(mapping.Binding.ToString(), GetBindingValue(outputBindingElement));
        
        var outputSettingElement = outputBindingElement.Parent.Element(mapping.Settings[0].Name);
        Assert.NotNull(outputSettingElement, nameof(outputSettingElement));
        Assert.AreNotEqual(inputSettingValue, outputSettingElement.GetAttribute("Value"));
        Assert.AreEqual(mapping.Settings[0].Value, outputSettingElement.GetAttribute("Value"));
    }
    
    [Test]
    public async Task Update_creates_and_overwrites_bindings()
    {
        // NOTE tests creation, overwriting

        // Arrange
        // use default mapping data
        this.Arrange_Default_MappingData(false);
        // preserve the both bindings for Ship-Weapons-CycleFireGroupPrevious
        var mapping = this._source.Mappings.Single(m => m.Id == "Ship-Throttle-ForwardKey");
        mapping.Primary.Preserve = true;
        mapping.Secondary.Preserve = true;
        // get the Primary binding in the xml and remove it
        var bindingElement = this.GetElementForBinding(this._inputXml, mapping, nameof(mapping.Primary));
        bindingElement.Remove();
        // get the secondary binding in the xml and make sure it's different
        bindingElement = this.GetElementForBinding(this._inputXml, mapping, nameof(mapping.Secondary));
        RandomizeBindingValue(bindingElement);
        var inputSecondaryBindingValue = GetBindingValue(bindingElement);

        // Act
        var changed = await this.Act();

        // Assert
        Assert.IsTrue(changed, nameof(changed));
        this.AssertBasics();

        // same value, just recreated
        var createdPrimaryBindingElement = this.GetElementForBinding(this._outputXml, mapping, nameof(mapping.Primary));
        Assert.NotNull(createdPrimaryBindingElement, nameof(createdPrimaryBindingElement));
        Assert.AreEqual(mapping.Primary.ToString(), GetBindingValue(createdPrimaryBindingElement));

        // different value, overwritten
        Assert.AreNotEqual(mapping.Secondary.ToString(), inputSecondaryBindingValue);
        var overwrittenSecondaryBindingElement = this.GetElementForBinding(this._outputXml, mapping, nameof(mapping.Secondary));
        Assert.NotNull(overwrittenSecondaryBindingElement, nameof(overwrittenSecondaryBindingElement));
        Assert.AreEqual(mapping.Secondary.ToString(), GetBindingValue(overwrittenSecondaryBindingElement));
    }

    [Test]
    public async Task Update_creates_mapping()
    {
        // Arrange
        // use default mapping data
        this.Arrange_Default_MappingData(false);
        // choose a mapping to preserve
        var mapping = this._source.Mappings.Single(m => m.Id == "Ship-Cooling-ToggleButtonUpInput");
        mapping.Primary.Preserve = true;
        mapping.Secondary.Preserve = true;
        mapping.Settings[0].Preserve = true;
        // completely remove the mapping from the xml
        var element = GetElementForMapping(this._inputXml, mapping);
        element.Remove();

        // Act
        var changed = await this.Act();

        // Assert
        Assert.IsTrue(changed, nameof(changed));
        this.AssertBasics();

        var createdMappingElement = GetElementForMapping(this._outputXml, mapping);
        Assert.NotNull(createdMappingElement, nameof(createdMappingElement));
        AssertED.AreEqual(createdMappingElement, mapping);
    }
    
    // TODO test ignore mapping change (not preserved)
    // TODO test overwriting setting
    // TODO test creating setting
    // TODO test ignoring setting
}