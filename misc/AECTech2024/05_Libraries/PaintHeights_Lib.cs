// #! csharp
using System;
using System.Linq;
using System.Collections.Generic;

using Rhino;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Commands;

class Brush : BrushTools.BaseBrush
{ 
    readonly RhinoObject _bplane;

    public Brush(RhinoObject brushPlane)
    {
        _bplane = brushPlane;
    }

    protected override IEnumerable<Mesh> GetPaintSurfaces() => _bplane.GetMeshes(MeshType.Render);

    protected override void ApplyBrush(RhinoDoc doc, Point3d at, bool reverse)
    {
        foreach (RhinoObject robj in doc.Objects
                                        .FindByObjectType(ObjectType.Extrusion))
        {
            var bbox = robj.Geometry.GetBoundingBox(true);
            var pp = bbox.Min;
            double dist = at.DistanceTo(pp);
            if (dist < m_falloff)
            {
                double height = (bbox.Max - bbox.Min).Z;
                double value = ComputeValue(dist, height, reverse);

                var xform = Transform.Scale(Plane.WorldXY, 1.0d, 1.0d, value / height);
                doc.Objects.Transform(robj, xform, true);
            }
        }
    }
}


var s = RhinoGet.GetOneObject("Select Brush Plane", false, ObjectType.Brep | ObjectType.Mesh, out ObjRef objRef);
if (s == Result.Success)
{
    RhinoObject brushPlane = objRef.Object();
    var brush = new Brush(brushPlane);
    brush.DoPaint();
}
