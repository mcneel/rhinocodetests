# https://developer.rhino3d.com/guides/rhinopython/python-overloads/
import clr
import System
import Rhino.Geometry as rg

srf = rg.NurbsSurface.CreateFromSphere(
    rg.Sphere(rg.Point3d(1.0, 1.0, 0.0), 10.0)
)

cv_param_3d = rg.Point3d()

res, cv_res_3d = srf.Points.GetPoint(0, 0)
cv_param_4d_ref = clr.Reference[rg.Point4d]()
res = srf.Points.GetPoint(0, 0, cv_param_4d_ref)
res, cv_res_def = srf.Points.GetPoint(0, 0)

# <class 'Rhino.Geometry.Point3d'> <class 'Rhino.Geometry.Point3d'>
print(type(cv_param_3d), type(cv_res_3d))

# 0,0,0 1,1,-10
print(cv_param_3d, cv_res_3d)

print(cv_param_4d_ref.Value, type(cv_param_4d_ref))

# 0,0,0,0 1,1,-10,1
print(cv_param_4d_ref.Value)

# <class 'Rhino.Geometry.Point3d'>
print(type(cv_res_def))

# 1,1,-10
print(cv_res_def)

getpoint4d = srf.Points.GetPoint.Overloads[
    System.Int32,
    System.Int32,
    rg.Point4d().GetType().MakeByRefType()
]
# 1,1,-10,1
res, point = getpoint4d(0, 0)
print(f"{point} type:({type(point)})")
