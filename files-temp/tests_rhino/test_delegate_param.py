import rhinoscriptsyntax as rs
import Rhino


def test():

    id = None

    def ArcFilter(rhino_object, geometry, component_index):
        """
        check that the object is both an arc not the other input curve
        """
        ret, arc = geometry.ToNurbsCurve().TryGetArc()
        if id is None:
            if ret:
                return True
            else:
                print("Curve must be an arc.")
        else:
            if ret:
                if rhino_object.Id != id:
                    return True
            else:
                print("Curve must be an arc.")
        return False

    id = rs.GetObject()

    ga = Rhino.Input.Custom.GetObject()
    ga.GeometryFilter = Rhino.DocObjects.ObjectType.Curve
    ga.SetCustomGeometryFilter(ArcFilter)
    ga.EnablePreSelect(False, True)
    ga.SetCommandPrompt(
        "Select an arc that is not the same curve as previously selected"
    )

    ret = ga.Get()

    if (ga.CommandResult() != Rhino.Commands.Result.Success):
        return

    if ret == Rhino.Input.GetResult.Object:
        arc = ga.Object(0).Geometry().ToNurbsCurve().TryGetArc()[1]
        print(arc)


if __name__ == "__main__":
    test()
