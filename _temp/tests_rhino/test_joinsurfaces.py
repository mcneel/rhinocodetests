#! python3

import rhinoscriptsyntax as rs

import System
import System.Collections.Generic
import Rhino


# rc, things = Rhino.Input.RhinoGet.Get"prompt", False, Rhino.DocObjects.ObjectType.AnyObject)
go = Rhino.Input.Custom.GetObject()
rc = go.GetMultiple(2, 0)
ids = [go.Object(i) for i in range(go.ObjectCount)]
x = rs.JoinSurfaces(ids, True, True)
print(x)
