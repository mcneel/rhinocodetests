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
print(type(p1))

gp.Get()
p2 = gp.Point()
print(type(p2))

v = p1 - p2

assert "Vector3d" in repr(type(v))
