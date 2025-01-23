#! python3

import Rhino.Geometry as RG
from ghpythonlib.components import CullDuplicates

points = []
points.append(RG.Point3d.Origin)
points.append(RG.Point3d.Origin)

a = CullDuplicates(points, 100)

result = a['points'] is not None