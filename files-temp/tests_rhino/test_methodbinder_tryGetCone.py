import Rhino
import scriptcontext as sc
import rhinoscriptsyntax as rs


def test():
    tol = sc.doc.ModelAbsoluteTolerance
    id = rs.GetObject(filter=8)
    srf = rs.coercesurface(id).UnderlyingSurface()
    type_rc, x = Rhino.Geometry.NurbsSurface.TryGetCone(srf,tol)
    print(type_rc, x)


if __name__ == "__main__":
    test()