using NUnit.Framework;
using SCCM.Core;
using SCCM.Tests.Mocks;
using System.Xml;
using System.Xml.Linq;

namespace SCCM.Tests;

[TestFixture]
public class MappingUpdater_Update_Tests
{
    private readonly MappingUpdater _updater;
    private readonly IPlatform _platform;
    private readonly IFolders _folders;

    private string GetTestXmlPath()
    {
        return new System.IO.FileInfo(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "actionmaps.xml")).FullName;
    }


    public MappingUpdater_Update_Tests()
    {
        this._platform = new PlatformForTest(DateTime.UtcNow);
        this._folders = new FoldersForTest();
        System.IO.File.Copy(Samples.GetActionMapsXmlPath(), this.GetTestXmlPath(), true);
        this._updater = new MappingUpdater(this._platform, this._folders, this.GetTestXmlPath());
    }

    [Test]
    public async Task Update_updates_actionmapsxml()
    {
        /* CHANGES
         * - switched joystick order
         */
        var data = new MappingData
        {
            Inputs = new InputDevice[] {
                new InputDevice { Type = "joystick", Instance = 1, Product = " VKBsim Gladiator EVO OT  L SEM   {3205231D-0000-0000-0000-504944564944}", Settings = new InputDeviceSetting[] {
                    new InputDeviceSetting { Name = "flight_move_strafe_vertical", Properties = new Dictionary<string, string> { { "invert", "1" } } },
                    new InputDeviceSetting { Name = "flight_move_strafe_longitudinal", Properties = new Dictionary<string, string> { { "invert", "1" } } },
                } },
                new InputDevice { Type = "joystick", Instance = 2, Product = " VKB-Sim Gladiator NXT R    {0200231D-0000-0000-0000-504944564944}" },
            }
        };
        await this._updater.Update(data);


        XDocument? xd = null;
        using (var fs = new FileStream(this.GetTestXmlPath(), FileMode.Open))
        {
            var ct = new CancellationToken();
            xd = await XDocument.LoadAsync(fs, LoadOptions.None, ct);
        }

        // TODO check XDocument
    }
}