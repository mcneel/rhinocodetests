using System;
using System.IO;
using System.Linq;
using System.Diagnostics;

using Rhino;
using Rhino.Geometry;
using Rhino.FileIO;


var tempfile = Path.GetTempFileName().Replace(".tmp", ".3dm");
Console.WriteLine(tempfile);

var r3dm = new File3dm();
r3dm.Objects.AddCircle(new Circle(12.0));
r3dm.Write(tempfile, new File3dmWriteOptions());

using (File3dm doc = File3dm.Read(tempfile))
{
    foreach(var obj in doc.Objects)
    {
        Console.WriteLine($"{obj is null}");
        Console.WriteLine($"Swear to god there is one obj here that is \"{obj}\"");
    }

    var first = doc.Objects.ElementAt(0);
    if (first is null)
    {
        throw new Exception();
    }
}