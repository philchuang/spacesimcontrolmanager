using System.Xml.Linq;
using NUnit.Framework;
using SSCM.Core;
using SSCM.Tests;

namespace SSCM.Elite.Tests;

public static class AssertED
{
    public static void AreEqual(EDMappingData? expected, EDMappingData? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.ReadTime, actual.ReadTime, nameof(expected.ReadTime));
        Assert2.EnumerableEquals(expected.Mappings, actual.Mappings, AreEqual);
        Assert2.EnumerableEquals(expected.Settings, actual.Settings, AreEqual);
    }

    public static void AreEqual(EDMappingSetting? expected, EDMappingSetting? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Group, actual.Group, nameof(expected.Group));
        Assert.AreEqual(expected.Name, actual.Name, nameof(expected.Name));
        Assert.AreEqual(expected.Preserve, actual.Preserve, nameof(expected.Preserve));
        Assert.AreEqual(expected.Value, actual.Value, nameof(expected.Value));
    }

    public static void AreEqual(EDMapping? expected, EDMapping? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Group, actual.Group, nameof(expected.Group));
        Assert.AreEqual(expected.Name, actual.Name, nameof(expected.Name));
        AreEqual(expected.Binding, actual.Binding);
        AreEqual(expected.Primary, actual.Primary);
        AreEqual(expected.Secondary, actual.Secondary);
        Assert2.EnumerableEquals(expected.Settings, actual.Settings, AreEqual);
    }

    public static void AreEqual(EDBinding? expected, EDBinding? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Preserve, actual.Preserve, nameof(expected.Preserve));
        AreEqual(expected.Key, actual.Key);
        Assert2.EnumerableEquals(expected.Modifiers, actual.Modifiers, AreEqual);
    }

    public static void AreEqual(EDBindingKey? expected, EDBindingKey? actual)
    {
        if (expected == null && actual == null) return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        // silly unreachable code to get rid of warnings
        if (expected == null) return;
        if (actual == null) return;

        Assert.AreEqual(expected.Device, actual.Device, nameof(expected.Device));
        Assert.AreEqual(expected.Key, actual.Key, nameof(expected.Key));
    }

    public static void AreEqual(XElement? mappingElement, EDMapping? mapping)
    {
        if (mappingElement == null && mapping == null) return;

        Assert.NotNull(mappingElement, nameof(mappingElement));
        Assert.NotNull(mapping, nameof(mapping));
        
        var elements = mappingElement!.Elements().ToList();

        if (mapping!.Binding != null)
        {
            var bindingElement = elements.SingleOrDefault(e => string.Equals(nameof(mapping.Binding), e.Name.LocalName));
            Assert.NotNull(bindingElement, nameof(bindingElement));
            AreEqual(bindingElement, mapping.Binding);
            elements.Remove(bindingElement!);
        }
        else
        {
            var primaryElement = elements.SingleOrDefault(e => string.Equals(nameof(mapping.Primary), e.Name.LocalName));
            Assert.NotNull(primaryElement, nameof(primaryElement));
            AreEqual(primaryElement, mapping.Primary);
            elements.Remove(primaryElement!);
            
            var secondaryElement = elements.SingleOrDefault(e => string.Equals(nameof(mapping.Secondary), e.Name.LocalName));
            Assert.NotNull(secondaryElement, nameof(secondaryElement));
            AreEqual(secondaryElement, mapping.Secondary);
            elements.Remove(secondaryElement!);
        }

        if (elements.Count == 0 && mapping.Settings.Count == 0) return;

        Assert.AreEqual(mapping.Settings.Count, elements.Count);

        var settingElementsMap = elements.ToDictionary(e => e.Name.LocalName);
        var settingsMap = mapping.Settings.ToDictionary(s => s.Name);

        Assert2.DictionaryEquals(settingElementsMap, settingsMap, false, (e, s) => AreEqual(e, s));
    }

    public static void AreEqual(XElement? bindingElement, EDBinding? binding)
    {
        if (bindingElement == null && binding == null) return;

        Assert.NotNull(bindingElement, nameof(bindingElement));
        Assert.NotNull(binding, nameof(binding));

        AreEqual(bindingElement, binding!.Key);

        var modifierElements = bindingElement!.Elements().Where(e => string.Equals("Modifier", e.Name.LocalName)).ToList();
        Assert.AreEqual(modifierElements.Count, binding.Modifiers.Count);

        for (var i = 0; i < modifierElements.Count; i++)
        {
            AreEqual(modifierElements[i], binding.Modifiers[i]);
        }
    }

    public static void AreEqual(XElement? bindingElement, EDBindingKey bindingKey)
    {
        if (bindingElement == null && bindingKey == null) return;

        Assert.NotNull(bindingElement, nameof(bindingElement));
        Assert.NotNull(bindingKey, nameof(bindingKey));

        var device = bindingElement!.GetAttribute("Device");
        var key = bindingElement!.GetAttribute("Key");

        Assert.AreEqual(bindingKey.Device, device);
        Assert.AreEqual(bindingKey.Key, key);
    }

    public static void AreEqual(XElement? settingElement, EDMappingSetting setting)
    {
        if (settingElement == null && setting == null) return;

        Assert.NotNull(settingElement, nameof(settingElement));
        Assert.NotNull(setting, nameof(setting));

        var value = settingElement.GetAttribute("Value");

        Assert.AreEqual(setting.Value, value);
    }
}