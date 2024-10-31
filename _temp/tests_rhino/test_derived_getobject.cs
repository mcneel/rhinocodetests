using System;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

var go = new CustomGetObject();

go.GroupSelect = false;
go.AcceptNothing(true);

var r = go.Get();
Console.WriteLine($"Object: {r}");


class CustomGetObject: Rhino.Input.Custom.GetObject
{ 
    public override bool CustomGeometryFilter(
        RhinoObject rhObject,
        GeometryBase geometry,
        ComponentIndex componentIndex) => true;
}