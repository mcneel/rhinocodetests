from Rhino.Input.Custom import *
from Rhino.Commands import *
import Rhino

def get_curve():
    gc = GetObject()
    gc.GeometryFilter = Rhino.DocObjects.ObjectType.Curve
    gc.Get()
    if gc.CommandResult()!=Rhino.Commands.Result.Success: return
    crv = gc.Object(0).Curve()
    return crv

crv = get_curve()

pts = crv.Points

int1 = 0

print(pts.GetPoint(int1))