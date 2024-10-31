#! python3
import sys
import os.path as op

import System
from System import Guid
from System.Reflection import Assembly
import Rhino
from Rhino import RhinoApp
from Rhino.PlugIns import PlugIn as P

assm = Assembly.Load("RhinoCommon")
rhino = op.dirname(assm.Location)
grasshopper = op.join(rhino, r"Plug-ins\Grasshopper\GrasshopperPlugin.rhp")
print(grasshopper)

res, ghId = P.LoadPlugIn(grasshopper)
print(res, ghId)
gh = RhinoApp.GetPlugInObject(ghId)
print(gh)

res, ghId = P.LoadPlugIn(grasshopper)
print(res, ghId)
gh = RhinoApp.GetPlugInObject(ghId)
print(gh)

gh_id = Guid("B45A29B1-4343-4035-989E-044E8580D9CF")
gh = RhinoApp.GetPlugInObject(gh_id)
print(gh)
