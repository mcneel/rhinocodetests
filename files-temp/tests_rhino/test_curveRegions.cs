// Create a Rhino mesh
using System;
using System.Diagnostics;

using Rhino;

var doc = RhinoDoc.ActiveDoc;

Rhino.Input.RhinoGet.GetOneObject("Select self intersecting curve", false, Rhino.DocObjects.ObjectType.Curve, out Rhino.DocObjects.ObjRef objRef);

Rhino.Geometry.Curve[] curves = new Rhino.Geometry.Curve[]
{
    objRef.Curve(),
};

curves[0].TryGetPlane(out Rhino.Geometry.Plane plane);

var regions = Rhino.Geometry.Curve.CreateBooleanRegions(curves, plane, false, doc.ModelAbsoluteTolerance);

for(int i = 0; i < regions.RegionCount; i++)
{
    Rhino.Geometry.Curve[] boundaries = regions.RegionCurves(i);

    for(int j = 0; j < regions.BoundaryCount(i); j++)
    {
        Rhino.Geometry.PolyCurve pc = new Rhino.Geometry.PolyCurve();

        for(int k = 0; k < regions.SegmentCount(i, j); k++)
        {
            int idx = regions.SegmentDetails(i, j, k, out Rhino.Geometry.Interval interval, out bool rev);

            var dup = boundaries[idx].DuplicateCurve();
            dup.Trim(interval);
            if(rev)
            {
                dup.Reverse();
            }

            pc.Append(dup);
        }

        doc.Objects.AddCurve(pc);
    }
}