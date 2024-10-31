import Rhino
from Rhino.Input.Custom import *
from Rhino.Commands import *
from Rhino.Geometry import Point3d, PolylineCurve

def get_curve():
    gc = GetObject()
    gc.GeometryFilter = Rhino.DocObjects.ObjectType.Curve
    gc.Get()
    if gc.CommandResult()!=Rhino.Commands.Result.Success: return
    crv = gc.Object(0).Curve()
    return crv

crv = get_curve()

dummy_ply = PolylineCurve()

print( crv.ToPolyline(0.1, 0.0, 0.0, 0.0))