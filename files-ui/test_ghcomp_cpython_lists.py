import rhinoscriptsyntax as rs
import ghpythonlib.components as ghcomp
import scriptcontext as sc

points = rs.GetPoints(True, True)
if points:
    curves = ghcomp.Voronoi(points)
    for curve in curves:
        sc.doc.Objects.AddCurve(curve)
    for point in points:
        sc.doc.Objects.AddPoint(point)
    sc.doc.Views.Redraw()