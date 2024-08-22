#! python3
import Eto.Forms
import Eto.Drawing
import Rhino

class CustomDrawable(Eto.Forms.Drawable):
    def __init__(self):
        super().__init__()
        self._hover = False
        self._mouse_click = False

    def OnMouseMove(self, e):
        Rhino.RhinoApp.WriteLine("OnMouseMove")

    def OnMouseEnter(self, e):
        Rhino.RhinoApp.WriteLine("OnMouseEnter")

    def OnMouseLeave(self, e):
        Rhino.RhinoApp.WriteLine("OnMouseLeave")

    def OnMouseDown(self, e):
        Rhino.RhinoApp.WriteLine("OnMouseDown")

    def OnMouseUp(self, e):
        Rhino.RhinoApp.WriteLine("OnMouseUp")

