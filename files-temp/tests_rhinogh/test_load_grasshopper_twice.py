#! python3
import sys
import os.path as op

import System
from System.Reflection import Assembly
import Rhino
from Rhino import RhinoApp
from Rhino.PlugIns import PlugIn as P


rhino = op.dirname(Assembly.GetExecutingAssembly().Location)
grasshopper = op.join(rhino, r"Plug-ins\Grasshopper\GrasshopperPlugin.rhp")
print(grasshopper)

res, ghId = P.LoadPlugIn(grasshopper)
gh = RhinoApp.GetPlugInObject(ghId)
print(gh)

res, ghId = P.LoadPlugIn(grasshopper)
gh = RhinoApp.GetPlugInObject(ghId)
print(gh)
