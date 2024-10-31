// #! csharp
// https://www.nuget.org/packages/EasingFunctions
// https://github.com/emmauss/easing-functions
#r "nuget: EasingFunctions, 1.0.1"
using System;
using System.Linq;
using SD = System.Drawing;
using EF = Eto.Forms;

using Rhino;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Commands;

class Brush : GetPoint
{ 
    readonly RhinoObject _bsurface;

    // brush option indices
    readonly int sizeIndex;
    readonly int fosizeIndex;
    readonly int strengthIndex;
    
    // default values for brush options
    double _size = 8f;
    double _falloff = 16f;
    double _strength = 0.1f;

    // brush effect range
    double _maxValue = 60f;
    double _minValue = 1f;
    
    bool _mouseDown = false;
    Easing.Ease _ease;

    public Brush(RhinoObject brushSurface)
    {
        this.SetCommandPrompt("Paint");

        _bsurface = brushSurface;
        _ease = new Easing.Quartic(new Easing.Vector(Convert.ToSingle(_falloff), Convert.ToSingle(_strength)));

        // define the options using default values, and a valid range for each
        var optsizedouble = new OptionDouble(_size, 1, 100);
        var optfalloffdouble = new OptionDouble(_falloff, 1, 100);
        var optstrengthdouble = new OptionDouble(_strength, 0.1, 10);

        // add the options to this getter instance
        sizeIndex     = this.AddOptionDouble("BrushSize", ref optsizedouble, "Enter Brush Size");
        fosizeIndex   = this.AddOptionDouble("FalloffSize", ref optfalloffdouble, "Enter Falloff Size");
        strengthIndex = this.AddOptionDouble("Strength", ref optstrengthdouble, "Enter Brush Strength");
    }

    public void DoPaint()
    {
        GetResult res;
        do 
        {
            res = this.Get(onMouseUp: true);
            switch (res)
            {
                case GetResult.Option:
                	// one of brush options was changed
                    var opt = this.Option();
                    if (opt.Index == sizeIndex)
                    {
                        _size = opt.CurrentNumericValue;
                    }
                    else
                    if (opt.Index == fosizeIndex)
                    {
                        _falloff = opt.CurrentNumericValue;
                        _ease = new Easing.Quartic(new Easing.Vector(Convert.ToSingle(_falloff), Convert.ToSingle(_strength)));
                    }
                    else
                    if (opt.Index == strengthIndex)
                    {
                        _strength = opt.CurrentNumericValue;
                        _ease = new Easing.Quartic(new Easing.Vector(Convert.ToSingle(_falloff), Convert.ToSingle(_strength)));
                    }
                    break;

                case GetResult.Point:
                    // use click in viewport; lets continue the loop
                    break;
            }
        } while (res != GetResult.Cancel); // user hit ESC key
    }

    protected override void OnMouseMove(GetPointMouseEventArgs e)
    {
        _mouseDown = e.LeftButtonDown;
    }

    protected override void OnDynamicDraw(GetPointDrawEventArgs e)
    {
        if (e.Viewport is null)
        {
            return;
        }

        Point3d p = e.CurrentPoint;
        Point2d cp = e.Viewport.WorldToClient(p);
        if (e.Viewport.GetFrustumLine(cp.X, cp.Y, out Line line))
        {
            line.Flip();
            Ray3d ray = new Ray3d(line.PointAt(0), line.Direction);

            double closestOnMesh = double.MaxValue;
            Circle head = default;
            Circle falloff = default;

            foreach (Mesh mesh in _bsurface.GetMeshes(MeshType.Render)) 
            {
                double t = Rhino.Geometry.Intersect.Intersection.MeshRay(mesh, ray);
                if (t >= 0)
                {
                    closestOnMesh = t;
                    Point3d hitPoint = ray.PointAt(t);
                    MeshPoint mp = mesh.ClosestMeshPoint(hitPoint, double.MaxValue);
                    Vector3d norm = mesh.NormalAt(mp);
                    Plane headPlane = new Plane(hitPoint, norm);
                    head = new Circle(headPlane, hitPoint, _size);
                    falloff = new Circle(headPlane, hitPoint, _falloff);
                }
            }

            if (closestOnMesh != double.MaxValue)
            {
                e.Display.DrawCircle(head, SD.Color.Blue, 3);
                if (_falloff != _size)
                {
                    e.Display.DrawCircle(falloff, SD.Color.LightGray, 2);
                }
            }

            if (_mouseDown)
            {
                bool reverse = EF.Keyboard.Modifiers.HasFlag(EF.Keys.Shift);
                ApplyBrush(e.RhinoDoc, p, reverse);
            }
        }
    }

    void ApplyBrush(RhinoDoc doc, Point3d at, bool reverse)
    {
        foreach (RhinoObject robj in doc.Objects
                                        .FindByObjectType(ObjectType.Extrusion))
        {
            var bbox = robj.Geometry.GetBoundingBox(true);
            var pp = bbox.Min;
            double dist = at.DistanceTo(pp);
            if (dist < _falloff)
            {
                double height = (bbox.Max - bbox.Min).Z;
                double value = ComputeIncrement(dist, height, reverse);

                var xform = Transform.Scale(Plane.WorldXY, 1.0d, 1.0d, value / height);
                doc.Objects.Transform(robj, xform, true);
            }
        }
    }

    double ComputeIncrement(double dist, double start, bool reverse)
    {
        double value = start;
        double incr = _strength * (dist < _size ? 1.0d : _ease.Out(Convert.ToSingle(dist)));
        value += reverse ? -incr : incr;
        value = value > _maxValue ? _maxValue : value;
        value = value < _minValue ? _minValue : value;

        return value;
    }
}


var s = RhinoGet.GetOneObject("Select Brush Plane", false, ObjectType.Brep | ObjectType.Mesh, out ObjRef objRef);
if (s == Result.Success)
{
    RhinoObject brushPlane = objRef.Object();
    var brush = new Brush(brushPlane);
    brush.DoPaint();
}
