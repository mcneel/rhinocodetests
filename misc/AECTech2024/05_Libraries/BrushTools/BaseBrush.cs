#r "nuget: EasingFunctions, 1.0.1"
using System;
using System.Linq;
using System.Collections.Generic;
using SD = System.Drawing;
using EF = Eto.Forms;

using Rhino;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Commands;

namespace BrushTools
{
    public abstract class BaseBrush : GetPoint
    { 
        readonly int sizeIndex;
        readonly int fosizeIndex;
        readonly int strengthIndex;
        
        protected double m_size;
        protected double m_falloff;
        protected double m_strength;
        protected double m_maxValue = 60.0d;
        protected double m_minValue = 1.0d;
        protected bool _mouseDown = false;
        protected Easing.Ease m_ease;

        public BaseBrush(double size = 64.0d, double falloff = 128.0d, double strength = 0.5d)
        {
            m_size = size;
            m_falloff = falloff;
            m_strength = strength;
            m_ease = new Easing.Quartic(new Easing.Vector(Convert.ToSingle(m_falloff), Convert.ToSingle(m_strength)));

            this.SetCommandPrompt("Paint");

            var optsizedouble = new OptionDouble(m_size, 1, 256);
            var optfalloffdouble = new OptionDouble(m_falloff, 1, 256);
            var optstrengthdouble = new OptionDouble(m_strength, 0.1, 16);
            sizeIndex = this.AddOptionDouble("BrushSize", ref optsizedouble, "Enter Brush Size");
            fosizeIndex = this.AddOptionDouble("FalloffSize", ref optfalloffdouble, "Enter Falloff Size");
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
                        var opt = this.Option();
                        if (opt.Index == sizeIndex)
                        {
                            m_size = opt.CurrentNumericValue;
                        }
                        else if (opt.Index == fosizeIndex)
                        {
                            m_falloff = opt.CurrentNumericValue;
                        }
                        else if (opt.Index == strengthIndex)
                        {
                            m_strength = opt.CurrentNumericValue;
                        }
                        
                        m_ease = new Easing.Quartic(new Easing.Vector(Convert.ToSingle(m_falloff), Convert.ToSingle(m_strength)));
                        break;

                    case GetResult.Point:
                        // do the things
                        break;
                }
            } while (res != GetResult.Cancel);
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

                foreach (Mesh mesh in GetPaintSurfaces()) 
                {
                    double t = Rhino.Geometry.Intersect.Intersection.MeshRay(mesh, ray);
                    if (t >= 0)
                    {
                        closestOnMesh = t;
                        Point3d hitPoint = ray.PointAt(t);
                        MeshPoint mp = mesh.ClosestMeshPoint(hitPoint, double.MaxValue);
                        Vector3d norm = mesh.NormalAt(mp);
                        Plane headPlane = new Plane(hitPoint, norm);
                        head = new Circle(headPlane, hitPoint, m_size);
                        falloff = new Circle(headPlane, hitPoint, m_falloff);
                    }
                }

                if (closestOnMesh != double.MaxValue)
                {
                    e.Display.DrawCircle(head, SD.Color.Blue, 3);
                    if (m_falloff != m_size)
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

        protected double ComputeValue(double dist, double start, bool reverse)
        {
            double value = start;
            double incr = dist < m_size ? m_strength : m_ease.Out(Convert.ToSingle(m_falloff - dist));
            value += reverse ? -incr : incr;
            value = value > m_maxValue ? m_maxValue : value;
            value = value < m_minValue ? m_minValue : value;
            return value;
        }
    
        protected abstract IEnumerable<Mesh> GetPaintSurfaces();
        protected abstract void ApplyBrush(RhinoDoc doc, Point3d at, bool reverse);
    }
}
