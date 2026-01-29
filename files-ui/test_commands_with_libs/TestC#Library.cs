using System;
using TestAssembly.Math;

var m = new DoMath();
var v = m.Add(21, 21);
Console.WriteLine($"Testing C# Library: {v}");
Console.WriteLine($"Testing C# Library: {m.Solve()}");