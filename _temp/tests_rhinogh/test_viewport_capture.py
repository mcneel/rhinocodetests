#! python3

import os.path as op
import rhinoscriptsyntax as rs
import scriptcontext as sc

import System.Drawing as SD
import Rhino


path = op.dirname(__file__)

grasshopper = Rhino.RhinoApp.GetPlugInObject('Grasshopper')

grasshopper.SetSliderValue('00094c37-36a2-4a83-9cfb-942b5f040a29', 10)
grasshopper.RunSolver(True)

view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView

capture = Rhino.Display.ViewCapture()
capture.Width = view.ActiveViewport.Size.Width
capture.Height = view.ActiveViewport.Size.Height
capture.ScaleScreenItems = False
capture.DrawAxes = False
capture.DrawGrid = False
capture.DrawGridAxes = False
capture.TransparentBackground = False

counter = 0
for i in range(-50, 91, 5):
    grasshopper.SetSliderValue('00094c37-36a2-4a83-9cfb-942b5f040a29', i)
    grasshopper.EnableSolver()
    grasshopper.RunSolver(False)
    
    image: SD.Bitmap = capture.CaptureToBitmap(view)

    image.Save(op.join(path, f"image_{counter}.png"))
    counter += 1
