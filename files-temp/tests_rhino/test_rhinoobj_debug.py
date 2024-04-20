#! python3

import rhinoscriptsyntax as rs
import scriptcontext as sc

def Test():
    rhId = rs.GetObject()
    rhObj = rs.coercerhinoobject(rhId)
    print(rhObj)


Test()