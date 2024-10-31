#! python3
# https://www.nuget.org/packages/EasingFunctions
# https://github.com/emmauss/easing-functions
#r "nuget: EasingFunctions, 1.0.1"
import sys
import System
from System.Drawing import Color

import Rhino
from Rhino.Input import RhinoGet
from Rhino.Input.Custom import GetPoint, OptionDouble
from Rhino.DocObjects import ObjectType
from Rhino.Commands import Result
from Rhino.Geometry import Point3d, Ray3d, Circle, Plane, Transform, Line

from Eto.Forms import Keyboard, Keys

from Easing import Quartic, Vector

class Brush(GetPoint):
    def __init__(self, brush_surface):
        super().__init__()
        self.SetCommandPrompt("Paint")

        self._bsurface = brush_surface

        # Default values
        self._size = 68.0
        self._falloff = 128.0
        self._strength = 0.5
        self._maxValue = 60.0
        self._minValue = 1.0
        self._mouseDown = False

        self._ease = Quartic(Vector(self._falloff - self._size, self._strength))

        # Options with range
        self.optsizedouble = OptionDouble(self._size, 1.0, 100.0)
        self.optfalloffdouble = OptionDouble(self._falloff, 1.0, 100.0)
        self.optstrengthdouble = OptionDouble(self._strength, 0.1, 10.0)

        # Adding options
        self.sizeIndex, _ = self.AddOptionDouble("BrushSize", self.optsizedouble)
        self.fosizeIndex, _ = self.AddOptionDouble("FalloffSize", self.optfalloffdouble)
        self.strengthIndex, _ = self.AddOptionDouble("Strength", self.optstrengthdouble)

    def DoPaint(self):
        res = None
        while res != Rhino.Input.GetResult.Cancel:
            res = self.Get(True)
            if res == Rhino.Input.GetResult.Option:
                opt = self.Option()
                if opt.Index == self.sizeIndex:
                    self._size = self.optsizedouble.CurrentValue
                elif opt.Index == self.fosizeIndex:
                    self._falloff = self.optfalloffdouble.CurrentValue
                elif opt.Index == self.strengthIndex:
                    self._strength = self.optstrengthdouble.CurrentValue
                self._ease = Quartic(Vector(self._falloff - self._size, self._strength));

    def OnMouseMove(self, e):
        self._mouseDown = e.LeftButtonDown

    def OnDynamicDraw(self, e):
        if not e.Viewport:
            return
        
        p = e.CurrentPoint
        cp = e.Viewport.WorldToClient(p)
        success, line = e.Viewport.GetFrustumLine(cp.X, cp.Y)
        if success:
            line.Flip()
            ray = Ray3d(line.From, line.Direction)

            closest_on_mesh = sys.maxsize
            head = None
            falloff = None

            for mesh in self._bsurface.GetMeshes(Rhino.Geometry.MeshType.Render):
                t = Rhino.Geometry.Intersect.Intersection.MeshRay(mesh, ray)
                if t >= 0:
                    closest_on_mesh = t
                    hit_point = ray.PointAt(t)
                    mp = mesh.ClosestMeshPoint(hit_point, sys.maxsize)
                    norm = mesh.NormalAt(mp)
                    head_plane = Plane(hit_point, norm)
                    head = Circle(head_plane, self._size)
                    falloff = Circle(head_plane, self._falloff)

            if closest_on_mesh != sys.maxsize:
                e.Display.DrawCircle(head, Color.Blue, 3)
                if self._falloff != self._size:
                    e.Display.DrawCircle(falloff, Color.LightGray, 2)

            if self._mouseDown:
                reverse = Keyboard.Modifiers.HasFlag(Keys.Shift)
                self.ApplyBrush(e.RhinoDoc, p, reverse)

    def ApplyBrush(self, doc, at, reverse):
        for robj in doc.Objects.FindByObjectType(ObjectType.Extrusion):
            bbox = robj.Geometry.GetBoundingBox(True)
            pp = bbox.Min
            dist = at.DistanceTo(pp)
            if dist < self._falloff:
                height = bbox.Max.Z - bbox.Min.Z
                value = self.ComputeValue(dist, height, reverse)
                xform = Transform.Scale(Plane.WorldXY, 1.0, 1.0, value / height)
                doc.Objects.Transform(robj, xform, True)

    def ComputeValue(self, dist, start, reverse):
        value = start
        incr = self._strength if dist < self._size else self._ease.Out(self._falloff - dist);
        value += -incr if reverse else incr
        value = min(max(value, self._minValue), self._maxValue)
        return value


success, obj_ref = RhinoGet.GetOneObject("Select Brush Plane", False, filter=ObjectType.Brep | ObjectType.Mesh)
if success == Result.Success:
    brush_surface = obj_ref.Object()
    brush = Brush(brush_surface)
    brush.DoPaint()