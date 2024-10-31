#! python 3

import rhinoscriptsyntax as rs
import scriptcontext as sc

import System
import System.Collections.Generic
import Rhino

point = rs.AddPoint(5, 4, 3)
point = rs.coerce3dpoint(point)

# # select a brep
rhId = rs.GetObject()
rhObj = rs.coercerhinoobject(rhId)

geom = rhObj.Geometry
rc = geom.ClosestPoint(point, 0.0)
print(rc)

rc = geom.ClosestPoint(point, maximumDistance=0.0)
print(rc)
