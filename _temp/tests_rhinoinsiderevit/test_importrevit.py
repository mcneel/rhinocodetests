#! python3
import clr
clr.AddReference("RhinoInside.Revit")

from RhinoInside.Revit import Revit as R
from Rhino.Runtime.Code import RhinoCode

print(RhinoCode.TargetRuntime)

uidoc = R.ActiveUIDocument;
doc = R.ActiveDBDocument
print(uidoc)
print(doc)
