#! python3

import Rhino.Geometry as RG
from Rhino.Collections import Point3dList

p = Point3dList()

pt = RG.Point3d()
p.Add(pt)
p.Add(10, 10, 10)

result = p.Capacity == 4