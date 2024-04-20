import clr
import System
import Rhino.Geometry as rg

srf = rg.NurbsSurface.CreateFromSphere(
    rg.Sphere(rg.Point3d(1.0, 1.0, 0.0), 10.0)
)

cv_res_3d = clr.Reference[rg.Point3d]()
res = srf.Points.GetPoint(0, 0, cv_res_3d)
print(type(cv_res_3d.Value), cv_res_3d.Value)


cv_res_4d = clr.Reference[rg.Point4d]()
res = srf.Points.GetPoint(0, 0, cv_res_4d)
print(type(cv_res_4d.Value), cv_res_4d.Value)
