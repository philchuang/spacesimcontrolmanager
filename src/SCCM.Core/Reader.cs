﻿using System.Xml;
using System.Xml.Linq;

namespace SCCM.Core;

public class Reader
{
    public string ActionMapsXmlPath { get; private set; }

    public IReadOnlyList<InputDevice> Inputs { get; private set; } = new InputDevice[] {};

    public IReadOnlyList<Mapping> Mappings { get; private set; } = new Mapping[] {};

    public Reader(string path)
    {
        this.ActionMapsXmlPath = path;
    }

    public async Task Read()
    {
        this.Mappings = new List<Mapping>();

        using (var fs = new FileStream(this.ActionMapsXmlPath, FileMode.Open))
        using (var xr = XmlReader.Create(fs))
        {
            var ct = new CancellationToken();
            var root = await XDocument.ReadFromAsync(xr, ct);

            // TODO read into joysticks
            // TODO read into mappings
        }
    }
}
