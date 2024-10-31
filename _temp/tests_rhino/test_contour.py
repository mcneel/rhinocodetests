import Rhino
import scriptcontext as sc
import Rhino.Geometry as rg


def make_contour_curves():
    bfilter = Rhino.DocObjects.ObjectType.Brep
    rc, objrefs = Rhino.Input.RhinoGet.GetMultipleObjects(
        "Select breps", False, bfilter
    )
    if len(objrefs) == 0 or rc != Rhino.Commands.Result.Success:
        return rc
    print(len(objrefs))
    print("objects selected")
    for objref in objrefs:
        _bldg_geo = objref.Brep()
        bound_box = _bldg_geo.GetBoundingBox(rg.Plane.WorldXY)
        min_x = bound_box.Min.Z
        floor_base_pt = rg.Point3d(0, 0, min_x)
        section_plane = rg.Plane(floor_base_pt, rg.Vector3d.ZAxis)
        floor_crvs = rg.Brep.CreateContourCurves(_bldg_geo, section_plane)
        print(len(floor_crvs))
        for crv in floor_crvs:
            sc.doc.Objects.AddCurve(crv)

    sc.doc.Views.Redraw()
    return Rhino.Commands.Result.Success


if __name__ == "__main__":
    make_contour_curves()
