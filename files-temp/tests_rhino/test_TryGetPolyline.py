#! python 3

import Rhino
from Rhino.Geometry import LineCurve, Point2d

curve = LineCurve(Point2d(0, 0), Point2d(42, 42))

rc, polyline = curve.TryGetPolyline()
print("pass")

try:
    rc, polyline, parameters = curve.TryGetPolyline()
    print("fail")
except ValueError:
    print("pass")