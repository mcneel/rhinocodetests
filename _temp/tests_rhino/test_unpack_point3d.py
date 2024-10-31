#! python 3

import Rhino.Geometry as RG

p = RG.Point3d(1, 2, 3)

for x, y, z in [p]:
    print(x, y, z)