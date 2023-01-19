using NUnit.Framework;
using SCCM.Core;

namespace SCCM.Tests;

[TestFixture]
public class ReaderTests
{
    private readonly Reader _reader;

    private static string GetSamplesDir()
    {
        var working = System.IO.Directory.GetCurrentDirectory();
        return new System.IO.DirectoryInfo(System.IO.Path.Combine(working, "../../../samples")).FullName;
    }

    private static string GetSampleXmlPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(GetSamplesDir(), "actionmaps.3.18.4.xml")).FullName;
    }

    public ReaderTests()
    {
        _reader = new Reader(GetSampleXmlPath());
    }

    [Test]
    public async Task Read_LoadsData()
    {
        await _reader.Read();

        AssertEquals.ListEquals(new InputDevice[] {
            new InputDevice { Type = "keyboard", Instance = 1, Product = "Keyboard  {6F1D2B61-D5A0-11CF-BFC7-444553540000}" },
            new InputDevice { Type = "gamepad", Instance = 1, Product = "Controller (Gamepad)" },
            new InputDevice { Type = "joystick", Instance = 1, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" },
            new InputDevice { Type = "joystick", Instance = 2, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}" },
        },
        _reader.Inputs,
        (e, a) => {
            Assert.AreEqual(e.Type, a.Type);
            Assert.AreEqual(e.Instance, a.Instance);
            Assert.AreEqual(e.Product, a.Product);
        });
    }
}
