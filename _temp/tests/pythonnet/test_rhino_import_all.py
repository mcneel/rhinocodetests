from Rhino.Geometry import *

var = dict(locals())
for i in var:
    if 'Point' in i:
        print(i)

print(Vector3f)
print(Point2d)
print(Point2f)
print(Point3d)
