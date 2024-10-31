#! python 3
import os
import sys
import rhinoscriptsyntax as rs
import scriptcontext as sc
import ghpythonlib as gh
import math

import System
import System.Collections.Generic
import Rhino
import Rhino.Geometry as RG
import Grasshopper as GH


# python builtin
j = os.removedirs()                             # pass
os.removedirs()                                 # pass
os.path.dirname()                               # pass
a = os.path.dirname()                           # pass
math.asinh()                                    # pass
m = math.asinh()                                # pass

# rhino libraries
rs.AddEllipse()                                 # pass
m = rs.AddEllipse()                             # pass
gh.components.AlignPlanes()                     # pass

# rhino dotnet libraries
RG.Point3d.Add()                                # needs docstring
m = RG.Point3d.Add()                            # needs docstring
p: RG.Point3d = RG.Point3d()                    # improve docstring
p.Add()                                         # needs docstring
m = p.Add()                                     # needs docstring
GH.DataTree()                                   # improve docstring
g = GH.DataTree()                               # improve docstring

# other dotnet libs
System.Guid()                                   # needs docstring
g = System.Guid()                               # needs docstring

# misc
a = [str()];
a[0].capitalize()                               # needs docstring

Rhino.Render.ProxyTypes                         # needs docstring
GH                                              # pass