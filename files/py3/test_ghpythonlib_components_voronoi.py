#! python 3
import rhinoscriptsyntax as rs
import ghpythonlib.components as ghcomp
import scriptcontext as sc
import Rhino.Geometry as RG


points = [
    RG.Point3d(0, 0, 0),
    RG.Point3d(10, 10, 0),
    RG.Point3d(-10, 10, 0),
]

curves = ghcomp.Voronoi(points)
for curve in curves:
    sc.doc.Objects.AddCurve(curve)

for point in points:
    sc.doc.Objects.AddPoint(point)


curves_count = len(curves)
# print(curves_count)
result = curves_count == 3