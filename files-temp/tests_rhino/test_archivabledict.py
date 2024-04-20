# https://mcneel.slack.com/archives/C07BQBFBP/p1704206423002069

import Rhino
import scriptcontext


def main():
    gid = scriptcontext.doc.Objects.AddCircle(Rhino.Geometry.Circle(100))
    rob = Rhino.DocObjects.ObjRef(gid).Object()
    print(rob.UserDictionary)
    scriptcontext.doc.Objects.Delete(gid, False)


main()
