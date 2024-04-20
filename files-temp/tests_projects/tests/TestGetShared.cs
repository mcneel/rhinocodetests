// r "TestSharedAssembly"

using System;
using System.IO;

using Rhino.PlugIns;

using TestAssembly.Math;

var p = PlugIn.PathFromName("SomeGreatPlugin");
var plugin = Path.GetDirectoryName(p);
var data  = Path.Combine(plugin, "shared/data.txt");
Console.WriteLine(data);

var math = new DoMath();
Console.WriteLine($"Result: {math.Add(21, 21)}");

var m = new TestSharedAssembly.SharedMath.DoMath();
Console.WriteLine($"Result: {m.Multiply(21, 21)}");