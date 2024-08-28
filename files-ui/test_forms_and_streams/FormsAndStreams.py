#! python3
import Eto.Forms
import Eto.Drawing
import Rhino
import test

Rhino.RhinoApp.ClearCommandHistoryWindow()

test.test_print()
print("TEST")

counter = 0

def handler(s, e):
    global counter
    print(f"py3 test: {counter}")
    counter += 1

form = Eto.Forms.Form()
form.Owner = Rhino.UI.RhinoEtoApp.MainWindow
form.Size = Eto.Drawing.Size(400, 400)
form.Title = "Python 3"
form.MouseMove += handler 
form.Show()