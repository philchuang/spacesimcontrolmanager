using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace SSCM.Tests;

#pragma warning disable CS8604

public static class Extensions
{
    public static T JsonCopy<T>(this T self)
    {
        if (self == null) throw new ArgumentNullException(nameof(self));
        var json = JsonConvert.SerializeObject(self);
        var clone = JsonConvert.DeserializeObject<T>(json);
        return clone!;
    }

    private static Random _rnd = new Random();
    private const string ALPHA = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const string ALPHANUMERIC = ALPHA+"0123456789_";

    public static string RandomString(int length = 12)
    {
        var sb = new StringBuilder();
        sb.Append(ALPHA[_rnd.Next(ALPHA.Length)]);
        for (var i = 1; i < length; i++)
        {
            sb.Append(ALPHANUMERIC[_rnd.Next(ALPHANUMERIC.Length)]);
        }
        return sb.ToString();
    }

    public static async Task WriteToAsync(this XDocument self, string path)
    {
        System.IO.Directory.CreateDirectory(new FileInfo(path).DirectoryName);
        using (var fs = new FileStream(path, FileMode.Create))
        using (var xw = XmlWriter.Create(fs, new XmlWriterSettings { Async = true, Indent = true }))
        {
            var ct = new CancellationToken();
            await self.WriteToAsync(xw, ct);
        }
    }
}