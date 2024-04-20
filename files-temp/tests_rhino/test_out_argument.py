import Rhino
import rhinoscriptsyntax as rs
 
srf_id = rs.GetObject("Select surface", rs.filter.surface)
srf = rs.coercesurface(srf_id)
ns = srf.ToNurbsSurface()
 
for u in range(0, ns.Points.CountU):
    for v in range(0, ns.Points.CountV):
        res, pt3d = ns.Points.GetPoint(u, v)
        if res:
            print(pt3d)
 
for u in range(0, ns.Points.CountU):
    for v in range(0, ns.Points.CountV):
        res, pt4d = ns.Points.GetPoint(u, v)
        if res:
            print(pt4d)