import clr

clr.AddReference("System")
clr.AddReference("RhinoCommon")

import Rhino
import System.Drawing

print("Successfully executing cpython on Rhino")
Rhino.RhinoApp.WriteLine("Successfully executing cpython on Rhino");

def GetPointDynamicDrawFunc(sender, args):
    pt1 = Rhino.Geometry.Point3d(0.0, 0.0, 0.0)
    pt2 = Rhino.Geometry.Point3d(10.0, 10.0, 0.0)
    args.Display.DrawLine(pt1, args.CurrentPoint, System.Drawing.Color.Red, 2)
    args.Display.DrawLine(pt2, args.CurrentPoint, System.Drawing.Color.Blue, 2)


# Create an instance of a GetPoint class and add a delegate for the DynamicDraw event
gp = Rhino.Input.Custom.GetPoint()
gp.DynamicDraw += GetPointDynamicDrawFunc
gp.Get()
# removes the handler
gp.DynamicDraw -= GetPointDynamicDrawFunc
gp.Get()
