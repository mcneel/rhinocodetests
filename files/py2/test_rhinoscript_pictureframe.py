#! python 2
import os.path as op
import Rhino
import rhinoscriptsyntax as rs
import scriptcontext as sc
import System


path = op.join(op.dirname(__file__), 'test_rhinoscript_pictureframe.png')

with System.Drawing.Image.FromFile(path) as bitmap:
    unit_scale = 1.0
    if sc.doc.ModelUnitSystem != Rhino.UnitSystem.Inches:
        unit_scale = Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Inches, sc.doc.ModelUnitSystem)

    scale_x = 1.0 / bitmap.HorizontalResolution * unit_scale
    scale_y = 1.0 / bitmap.VerticalResolution * unit_scale

    width = bitmap.Width * scale_x
    height = bitmap.Height * scale_y

plane = Rhino.Geometry.Plane.WorldXY

# https://mcneel.myjetbrains.com/youtrack/issue/RH-84502
# no errors expected
rs.AddPictureFrame(plane, path, width, height)
result = True