namespace SCCM.Tests;

public static class Samples
{
    public static string GetDir()
    {
        var working = System.IO.Directory.GetCurrentDirectory();
        return new System.IO.DirectoryInfo(System.IO.Path.Combine(working, "../../../../../samples")).FullName;
    }

    public static string GetActionMapsXmlPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(GetDir(), "actionmaps.3.17.4.xml")).FullName;
    }

    public static string GetPartialMappingsJsonPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(GetDir(), "scmappings.3.17.4.partial.json")).FullName;
    }

}