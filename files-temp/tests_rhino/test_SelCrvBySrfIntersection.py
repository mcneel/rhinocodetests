#! python3

import rhinoscriptsyntax as rs
import scriptcontext as sc
import Rhino


def SelCrvBySrfIntersection():
    tol = sc.doc.ModelAbsoluteTolerance
    crvIds = rs.ObjectsByType(4, False, 1)

    if not crvIds:
        print("No curves found")
        return

    Objref = rs.GetObject("Select a surface", 8, preselect=True, subobjects=True)
    if not Objref:
        return

    idx = Objref.GeometryComponentIndex.Index
    if idx == -1:
        idx = 0
    face = Objref.Brep().Faces[idx]

    crvs = [rs.coercecurve(Id) for Id in crvIds]
    print(f"{len(crvs)} curves found")

    for i in range(len(crvs)):
        rc, intCrvs, intPts = Rhino.Geometry.Intersect.Intersection.CurveBrepFace(
            crvs[i], face, tol
        )

        print(intCrvs, intPts)
        if len(intCrvs) > 0 or len(intPts) > 0:
            rs.SelectObject(crvIds[i])


if __name__ == "__main__":
    SelCrvBySrfIntersection()
