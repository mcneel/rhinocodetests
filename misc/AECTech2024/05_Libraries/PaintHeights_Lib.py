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
from Rhino.Geometry import Point3d, Ray3d, Circle, Plane, Transform, Line, MeshType

from Eto.Forms import Keyboard, Keys

from Easing import Quartic, Vector

from BrushTools import BaseBrush

class Brush(BaseBrush):
    def __init__(self, brush_surface):
        super().__init__()

        self._bsurface = brush_surface

    def GetPaintSurfaces(self):
        return self._bsurface.GetMeshes(MeshType.Render)

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


success, obj_ref = RhinoGet.GetOneObject("Select Brush Plane", False, filter=ObjectType.Brep | ObjectType.Mesh)
if success == Result.Success:
    brush_surface = obj_ref.Object()
    brush = Brush(brush_surface)
    brush.DoPaint()