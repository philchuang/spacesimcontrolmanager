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
public class MappingExporter_Update_Tests
{
    private MappingExporter? _exporter;
    private readonly IPlatform _platform;
    private readonly EDFoldersForTest _folders;
    private EDMappingData _source = new EDMappingData();
    private XDocument? _originalXml = null;
    private XDocument? _updatedXml = null;

    public MappingExporter_Update_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._folders = new EDFoldersForTest();
    }

    private string TargetConfigPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TestContext.CurrentContext.Test.Name, "custom.4.0.binds")).FullName;
    }

    [SetUp]
    protected async Task Init()
    {
        this._folders.GameConfigPath = this.TargetConfigPath();
        this._exporter = new MappingExporter(this._platform, this._folders);
        this._exporter.StandardOutput += s => TestContext.Out.WriteLine($"[STD  ]\t{s}");
        this._exporter.DebugOutput    += s => TestContext.Out.WriteLine($"[DEBUG]\t{s}");
        this._exporter.WarningOutput  += s => TestContext.Out.WriteLine($"[WARN ]\t{s}");

        this._originalXml = await this.LoadXml(new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data", "custom40binds_0.xml")).FullName);
    }

    private async Task<bool> Act()
    {
        // write _originalXml
        System.IO.Directory.CreateDirectory(new FileInfo(this.TargetConfigPath()).DirectoryName);
        using (var fs = new FileStream(this.TargetConfigPath(), FileMode.Create))
        using (var xw = XmlWriter.Create(fs, new XmlWriterSettings { Async = true, Indent = true }))
        {
            var ct = new CancellationToken();
            await this._originalXml.WriteToAsync(xw, ct);
        }

        // execute export
        var changed = await this._exporter.Update(this._source);

        // read _updatedXml
        this._updatedXml = await this.LoadXml(this.TargetConfigPath());

        return changed;
    }

    private void AssertBasics()
    {
        Assert.NotNull(this._updatedXml, nameof(this._updatedXml));
        Assert.NotNull(this._updatedXml.Root, nameof(this._updatedXml.Root));
        Assert.AreEqual("Root", this._updatedXml.Root.Name.LocalName);
    }

    private async Task<XDocument> LoadXml(string path)
    {
        using (var fs = new FileStream(path, FileMode.Open))
        {
            var ct = new CancellationToken();
            return await XDocument.LoadAsync(fs, LoadOptions.None, ct);
        }
    }

    private XElement? GetInputElement(XDocument xd, string type, int instance)
    {
        return xd.XPathSelectElements($"/ActionMaps/ActionProfiles[@profileName='default']/options[@type='{type}' and @instance='{instance}']").SingleOrDefault();
    }

    private XElement? GetInputElement(XDocument xd, string type, int instance, string product)
    {
        return xd.XPathSelectElements($"/ActionMaps/ActionProfiles[@profileName='default']/options[@type='{type}' and @instance='{instance}' and @Product='{product}']").SingleOrDefault();
    }

    private XElement? GetMappingElement(XDocument xd, EDMapping mapping)
    {
        return xd.XPathSelectElements($"/Root/{mapping.Name}").SingleOrDefault();
    }

    private XElement? GetBindingElement(XDocument xd, EDMapping mapping, string ordinal)
    {
        return xd.XPathSelectElements($"/Root/{mapping.Name}/{ordinal}").SingleOrDefault();
    }

    private void RandomizeBindingValue(XElement bindingElement)
    {
        bindingElement.SetAttributeValue("Device", RandomString());
        bindingElement.SetAttributeValue("Key", RandomString());
        bindingElement.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase)).ToList().ForEach(RandomizeBindingValue);
    }

    private string GetBindingValue(XElement bindingElement)
    {
        return string.Join(
            " + ", 
            (new[] { bindingElement }.Concat(bindingElement.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName, StringComparison.OrdinalIgnoreCase)))
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
                new EDMapping("Ship-Cooling", "ToggleButtonUpInput") { Primary = new EDBinding("Keyboard", "Key_V"), Secondary = new EDBinding("231D3205", "Joy_15", new[] { new EDBindingKey("231D3205", "Joy_1"), new EDBindingKey("231D3205", "Joy_2") }) },
                new EDMapping("Ship-Weapons", "CycleFireGroupPrevious") { Primary = EDBinding.UNBOUND(), Secondary = new EDBinding("231D0200", "Joy_22") },
                new EDMapping("Ship-Throttle", "ForwardKey") { Primary = new EDBinding("Keyboard", "Key_W"), Secondary = new EDBinding("231D3205", "Joy_POV1Up") },
                new EDMapping("Ship-FlightRotation", "PitchAxisRaw") { Binding = new EDBinding("231D0200", "Joy_YAxis"), Settings = { new EDMappingSetting("Ship-FlightRotation-PitchAxisRaw", "Inverted", "1") } },
            }
        };

        this._source.Mappings.SelectMany(m => new[] { m.Primary, m.Secondary }).ToList().ForEach(m => { if (m != null) m.Preserve = preserve; });
        this._source.Mappings.SelectMany(m => m.Settings).ToList().ForEach(s => { s.Preserve = preserve; });
        this._source.Settings.ToList().ForEach(s => { s.Preserve = preserve; });
    }

    private (string, string, EDMapping, XElement) Arrange_Update_overwrites_binding()
    {
        // use default mapping data
        this.Arrange_Default_MappingData(false);
        // preserve the binding & setting
        var mapping = this._source.Mappings.Single(m => m.Id == "Ship-FlightRotation-PitchAxisRaw");
        mapping.Binding.Preserve = true;
        mapping.Settings[0].Preserve = true;
        // get the binding in the xml and make sure it's different
        var bindingElement = this.GetBindingElement(this._originalXml, mapping, nameof(mapping.Binding));
        RandomizeBindingValue(bindingElement);
        var originalBindingValue = GetBindingValue(bindingElement);
        // get the setting in the xml and make sure it's different
        var settingElement = bindingElement.Parent.Elements().First(e => e.Name.LocalName == "Inverted");
        settingElement.SetAttributeValue("Value", RandomString());
        var originalSettingValue = settingElement.GetAttribute("Value");

        return (originalBindingValue, originalSettingValue, mapping, bindingElement.Parent);
    }
    
    [Test]
    public async Task Update_overwrites_binding()
    {
        // NOTE also tests binding settings

        // Arrange
        var (originalBindingValue, originalSettingValue, mapping, mappingElement) = this.Arrange_Update_overwrites_binding();

        // Act
        var changed = await this.Act();

        // Assert
        Assert.IsTrue(changed, nameof(changed));
        this.AssertBasics();

        var changedBindingElement = this.GetBindingElement(this._updatedXml, mapping, "Binding");
        Assert.NotNull(changedBindingElement, nameof(changedBindingElement));
        Assert.AreNotEqual(originalBindingValue, GetBindingValue(changedBindingElement));
        Assert.AreEqual(mapping.Binding.ToString(), GetBindingValue(changedBindingElement));
        
        var changedSettingElement = changedBindingElement.Parent.Element(mapping.Settings[0].Name);
        Assert.NotNull(changedSettingElement, nameof(changedSettingElement));
        Assert.AreNotEqual(originalSettingValue, changedSettingElement.GetAttribute("Value"));
        Assert.AreEqual(mapping.Settings[0].Value, changedSettingElement.GetAttribute("Value"));
    }
    
    private (string, EDMapping, XElement) Arrange_Update_creates_and_overwrites_bindings()
    {
        // use default mapping data
        this.Arrange_Default_MappingData(false);
        // preserve the both bindings for Ship-Weapons-CycleFireGroupPrevious
        var mapping = this._source.Mappings.Single(m => m.Id == "Ship-Throttle-ForwardKey");
        mapping.Primary.Preserve = true;
        mapping.Secondary.Preserve = true;
        // get the primary binding in the xml and remove it
        var bindingElement = this.GetBindingElement(this._originalXml, mapping, nameof(mapping.Primary));
        bindingElement.Remove();
        // get the secondary binding in the xml and make sure it's different
        bindingElement = this.GetBindingElement(this._originalXml, mapping, nameof(mapping.Secondary));
        RandomizeBindingValue(bindingElement);
        var origSecondaryBindingValue = GetBindingValue(bindingElement);

        return (origSecondaryBindingValue, mapping, bindingElement.Parent);
    }
    
    [Test]
    public async Task Update_creates_and_overwrites_bindings()
    {
        // NOTE also tests modifiers

        // Arrange
        var (origSecondaryBindingValue, mapping, mappingElement) = this.Arrange_Update_creates_and_overwrites_bindings();

        // Act
        var changed = await this.Act();

        // Assert
        Assert.IsTrue(changed, nameof(changed));
        this.AssertBasics();

        // same value, just recreated
        var createdPrimaryBindingElement = this.GetBindingElement(this._updatedXml, mapping, nameof(mapping.Primary));
        Assert.NotNull(createdPrimaryBindingElement, nameof(createdPrimaryBindingElement));
        Assert.AreEqual(mapping.Primary.ToString(), GetBindingValue(createdPrimaryBindingElement));

        // different value, overwritten
        Assert.AreNotEqual(mapping.Secondary.ToString(), origSecondaryBindingValue);
        var overwrittenSecondaryBindingElement = this.GetBindingElement(this._updatedXml, mapping, nameof(mapping.Secondary));
        Assert.NotNull(overwrittenSecondaryBindingElement, nameof(overwrittenSecondaryBindingElement));
        Assert.AreEqual(mapping.Secondary.ToString(), GetBindingValue(overwrittenSecondaryBindingElement));
    }

    // TODO test ignore change (not preserved)
    // TODO test complete mapping created from scratch
}