using System.Xml.Linq;
using NUnit.Framework;

namespace SSCM.Tests;

public abstract class TestBase
{
    protected string TestDataDir => Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Data");
    protected string TestTempDir => Path.Combine(System.IO.Directory.GetCurrentDirectory(), TestContext.CurrentContext.Test.Name);

    protected TestBase()
    {
    }

    protected async Task<XDocument> LoadXml(string path)
    {
        using (var fs = new FileStream(path, FileMode.Open))
        {
            var ct = new CancellationToken();
            return await XDocument.LoadAsync(fs, LoadOptions.None, ct);
        }
    }

}