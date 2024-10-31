#! python3
# https://mcneel.myjetbrains.com/youtrack/issue/RH-81272

import rhinoscriptsyntax as rs
import scriptcontext as sc
import Rhino


def pl_crv_Z_filt(rhino_object, geometry, component_index):
    tol = rs.UnitAbsoluteTolerance()
    atol = Rhino.RhinoMath.DefaultAngleTolerance * 0.001
    zVec = Rhino.Geometry.Vector3d(0, 0, 1)
    planar, s_plane = geometry.TryGetPlane(tol * 0.1)
    if planar:
        # IsParallelTo returns int
        # this delegate must return boolean
        return s_plane.ZAxis.IsParallelTo(zVec, atol)
    else:
        return False


go = Rhino.Input.Custom.GetObject()
go.SetCommandPrompt("Select Planar Curve")
go.EnablePreSelect(True, True)
go.EnableHighlight(False)
go.SetCustomGeometryFilter(pl_crv_Z_filt)

go.Get()
