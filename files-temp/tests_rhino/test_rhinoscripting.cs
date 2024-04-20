#r "nuget: FsEx, 0.12.0"
#r "nuget: FSharp.Core, 4.5.2"
#r "nuget: Rhino.Scripting, 0.2.0"
using System;
using rs = Rhino.Scripting;

var corners = rs.GetRectangle();
Console.WriteLine(corners);
// var pl = rs.ViewCPlane( rs.PlaneFromPoints(corners[0], corners[1], corners[3]));
// rs.AddText("Hello, Ehsan", pl, height: 50.0);

