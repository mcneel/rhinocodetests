// r "..\tests_projects\libraries\TestAssembly\bin\TestAssembly.dll"

using System;
using TestAssembly.Math;

var math = new DoMath();

Console.WriteLine($"Result: {math.Add(21, 21)}");
