#! python3

import rhinoscriptsyntax as rs

import System
import System.Collections.Generic
import Rhino

bb = Rhino.Geometry.BoundingBox()
print(f"min: {bb.Min}")
print(f"max: {bb.Max}")

p = Rhino.Geometry.Point3d()
print(f"point: {p}")

p = Rhino.Geometry.Point3d(1, 2, 3)
print(f"point: {p}")