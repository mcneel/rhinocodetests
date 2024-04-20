#! python3

import rhinoscriptsyntax as rs
import scriptcontext as sc
import math

import System
import System.Collections.Generic
import Rhino

gp = Rhino.Input.Custom.GetPoint()

gp.Get()
p1 = gp.Point()

assert "Point3d" in repr(type(p1))
assert (p1 == p1) == True
assert (0  == p1) == False
assert (p1 == 0 ) == False
