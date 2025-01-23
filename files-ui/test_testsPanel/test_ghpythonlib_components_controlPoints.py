#! python 3
import Rhino
import ghpythonlib.components as ghc

x = [
    Rhino.Geometry.PolylineCurve([
        Rhino.Geometry.Point3d.Origin,
        Rhino.Geometry.Point3d(1,1,1),
        Rhino.Geometry.Point3d(5,5,5),
    ])
]

a = ghc.ControlPoints(x)

result = len(a.points) > 0