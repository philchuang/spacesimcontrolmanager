namespace SSCM.StarCitizen.Tests;

public static class Samples
{
    public static string GetDir()
    {
        var working = Directory.GetCurrentDirectory();
        return new DirectoryInfo(Path.Combine(working, "../../../../../samples/SC")).FullName;
    }

    public static string GetActionMapsXmlPath()
    {
        return new FileInfo(Path.Combine(GetDir(), "actionmaps.3.17.4.xml")).FullName;
    }

    public static string GetAttributesXmlPath()
    {
        return new FileInfo(Path.Combine(GetDir(), "attributes.3.17.5.xml")).FullName;
    }

    public static string GetPartialMappingsJsonPath()
    {
        return new FileInfo(Path.Combine(GetDir(), "scmappings.3.17.4.partial.json")).FullName;
    }

}