// r "RhinoCommon.dll"

using System;

var t = Type.GetType("Rhino.Geometry.Point3d, RhinoCommon");

Console.WriteLine(t?.ToString() ?? "NULL");