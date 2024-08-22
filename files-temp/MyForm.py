#! python3
import sys
import Eto.Forms
import Eto.Drawing
import Rhino

from MyControls import CustomDrawable

form = Eto.Forms.Form()
form.Owner = Rhino.UI.RhinoEtoApp.MainWindow
form.Content = CustomDrawable()
form.Size = Eto.Drawing.Size(600, 800)
form.Show()
