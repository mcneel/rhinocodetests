#! python3

import rhinoscriptsyntax as rs

import System
import System.Collections.Generic
import Rhino

# expect no errors
bb = Rhino.Geometry.BoundingBox()
p = Rhino.Geometry.Point3d()


result = True