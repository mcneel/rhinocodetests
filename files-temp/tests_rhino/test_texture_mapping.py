#! python 3

import rhinoscriptsyntax as rs
import scriptcontext as sc
import math

import clr
import System
import System.Collections.Generic
import Rhino

print(f"hi")

selob: Rhino.DocObjects.RhinoObject = [o for o in sc.doc.Objects if o.IsSelected(True)][0]

tfm = clr.StrongBox[Rhino.Geometry.Transform]()
tm = selob.GetTextureMapping(1, tfm)

print(tm)
print(20*"-")
print(tfm)
print(20*"*")

def do_box(tm):
    capped = clr.StrongBox[System.Boolean]()
    plane = clr.StrongBox[Rhino.Geometry.Plane]()
    dx = clr.StrongBox[Rhino.Geometry.Interval]()
    dy = clr.StrongBox[Rhino.Geometry.Interval]()
    dz = clr.StrongBox[Rhino.Geometry.Interval]()
    x = tm.TryGetMappingBox(plane, dx, dy, dz, capped)

    print(x)
    print(plane)
    print(dx)
    print(dy)
    print(dz)
    print(capped)

def do_cyl(tm):
    capped = clr.StrongBox[System.Boolean]()
    cylinder = clr.StrongBox[Rhino.Geometry.Cylinder]()
    x = tm.TryGetMappingCylinder(cylinder, capped)

    print(x)
    print(cylinder)
    print("\tBasePlane: " + str(cylinder.BasePlane))
    print("\tCenter: " + str(cylinder.Center))
    print("\tAxis: " + str(cylinder.Axis))
    print("\tHeight1: " + str(cylinder.Height1))
    print("\tHeight2: " + str(cylinder.Height2))
    print("\tTotalHeight: " + str(cylinder.TotalHeight))
    print(capped)

print(tfm)
do_cyl(tm)