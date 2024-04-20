"""
Note:
This project is work-in-progress and still in its infancy

- Reference to RhinoCommmon.dll is added by default

- You can specify your script requirements like:

    # r: <module name> [, <module name>]
    # requirements: <module name> [, <module name>]

    For example this line will ask the runtime to install
    the listed packages before running the script:

    # requirements: pytoml, keras
"""
#! python3

import System
import System.Collections.Generic
import Rhino
import Rhino.Geometry as g

s = g.Sphere(g.Point3d(1.0, 1.0, 1.0), 10.0)
res, lat, lng = s.ClosestParameter(g.Point3d(6.0, 6.0, 6.0))
print(f"{res=}, {lat=}, {lng=}")
