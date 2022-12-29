using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SCCM.Core.SC;

public class ActionMapsXmlHelper
{
    public XDocument Xml { get; init; }
    public string ActionMapsProfileName { get; init; }

    public ActionMapsXmlHelper(XDocument xd, string profileName)
    {
        this.Xml = xd;
        this.ActionMapsProfileName = profileName;
        this.Validate();
    }

    public static async Task<ActionMapsXmlHelper> Load(string path, string profileName)
    {
        if (!System.IO.File.Exists(path))
        {
            throw new FileNotFoundException($"Could not find the Star Citizen mappings file at [{path}]!");
        }

        using (var fs = new FileStream(path, FileMode.Open))
        {
            var ct = new CancellationToken();
            var xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);
            
            return new ActionMapsXmlHelper(xd, profileName);
        }
    }

    private void Validate()
    {
        if (this.Xml.Root == null)
        {
            throw new InvalidDataException($"Expecting <ActionMaps>, found nothing!");
        }

        if (!this.Xml.Root.Name.LocalName.Equals("ActionMaps"))
        {
            throw new InvalidDataException($"Expecting <ActionMaps>, found <{this.Xml.Root.Name.LocalName}>!");
        }

        if (this.Xml.XPathSelectElements($"/ActionMaps/ActionProfiles[@profileName='{this.ActionMapsProfileName}']").SingleOrDefault() == null)
        {
            throw new InvalidDataException($"Could not find <ActionProfiles> with profileName [{this.ActionMapsProfileName}].");
        }
    }

    public async Task Save(string path)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var xw = XmlWriter.Create(fs, new XmlWriterSettings { Async = true, Indent = true }))
        {
            var ct = new CancellationToken();
            await this.Xml.WriteToAsync(xw, ct);
        }
    }

    #region Helpers
    private string ActionProfilesXPath { get => $"/ActionMaps/ActionProfiles[@profileName='{this.ActionMapsProfileName}']"; }

    public static string GetOptionsTypeAbbv(string type)
    {
        return type switch {
            "joystick" => "js",
            "keyboard" => "kb",
            _ => throw new ArgumentOutOfRangeException(type),
        };
    }

    public static string GetOptionsTypeFromAbbv(string typeAbbv)
    {
        return typeAbbv switch {
            "js" => "joystick",
            "kb" => "keyboard",
            _ => throw new ArgumentOutOfRangeException(typeAbbv),
        };
    }

    public static string GetInputPrefixForOptionsElement(XElement options)
    {
        var type = options.GetAttribute("type");
        var instance = options.GetAttribute("instance");

        var typeAbbv = GetOptionsTypeAbbv(type);

        return $"{typeAbbv}{instance}_";
    }

    public static string GetInputPrefixForInputDevice(InputDevice input)
    {
        var typeAbbv = GetOptionsTypeAbbv(input.Type);
        var prefix = $"{typeAbbv}{input.Instance}_";
        return prefix;
    }

    private static Regex InputPrefixParseRegex = new Regex(@"^(\w+)(\d+)_.*$");

    public static (string, string) GetOptionsTypeAndInstanceForPrefix(string prefix)
    {
        var match = InputPrefixParseRegex.Match(prefix);
        var typeAbbv = match.Groups[1].Value;
        var instance = match.Groups[2].Value;
        var type = GetOptionsTypeFromAbbv(typeAbbv);
        return (type, instance);
    }

    public XElement? GetOptionsElementForInputDevice(InputDevice input)
    {
        return this.Xml.XPathSelectElements($"{this.ActionProfilesXPath}/options[@type='{input.Type}' and @instance='{input.Instance}' and @Product='{input.Product}']").SingleOrDefault();
    }

    public XElement? GetOptionsElementForInputTypeAndInstance(InputDevice input)
    {
        return this.GetOptionsElementForInputTypeAndInstance(input.Type, input.Instance.ToString());
    }

    public XElement? GetOptionsElementForInputTypeAndInstance(string type, string instance)
    {
        return this.Xml.XPathSelectElements($"{this.ActionProfilesXPath}/options[@type='{type}' and @instance='{instance}']").SingleOrDefault();
    }

    public XElement? GetOptionsElementForInputTypeAndProduct(InputDevice input)
    {
        return this.Xml.XPathSelectElements($"{this.ActionProfilesXPath}/options[@type='{input.Type}' and @Product='{input.Product}']").SingleOrDefault();
    }

    public XElement? GetElementForInputSetting(InputDevice input, string settingName)
    {
        return this.Xml.XPathSelectElements($"{this.ActionProfilesXPath}/options[@type='{input.Type}' and @instance='{input.Instance}' and @Product='{input.Product}']/{settingName}").SingleOrDefault();
    }

    public XElement? GetActionmapForMapping(Mapping mapping)
    {
        return this.Xml.XPathSelectElements($"{this.ActionProfilesXPath}/actionmap[@name='{mapping.ActionMap}']").SingleOrDefault();
    }

    public XElement? GetActionForMapping(Mapping mapping)
    {
        return this.Xml.XPathSelectElements($"{this.ActionProfilesXPath}/actionmap[@name='{mapping.ActionMap}']/action[@name='{mapping.Action}']").SingleOrDefault();
    }

    public List<XElement> GetAllActionRebindsForInputPrefix(string prefix)
    {
        return this.Xml.XPathSelectElements($"{this.ActionProfilesXPath}/actionmap/action/rebind[starts-with(@input, '{prefix}')]").ToList();
    }

    public List<XElement> GetAllActionRebindsForOptions(XElement options)
    {
        return this.GetAllActionRebindsForInputPrefix(GetInputPrefixForOptionsElement(options));
    }

    public void AddOptionsElement(XElement options)
    {
        var profile = this.Xml.XPathSelectElement($"{this.ActionProfilesXPath}");
        profile.Add(options);
    }

    public void AddActionmapElement(XElement actionmap)
    {
        var profile = this.Xml.XPathSelectElement($"{this.ActionProfilesXPath}");
        profile.Add(actionmap);
    }
    #endregion
}