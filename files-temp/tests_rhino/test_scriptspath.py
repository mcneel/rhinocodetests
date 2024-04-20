#! python 3
"""
This test depends on vray.py existing in
C:\ProgramData\McNeel\Rhinoceros\8.0\scripts
"""

# setup the vray.py file in system data dir
import os.path as op
import Rhino as R
sysDataDir = R.RhinoApp.GetDataDirectory(False, False)
vrayfile = op.join(sysDataDir, "scripts", "vray.py")
with open(vrayfile, "w") as vf:
    vf.write("def test(): print(\"works\")\n")

#setup the .pth file
pthfile = op.join(sysDataDir, "scripts", "python-3_vray.pth")
with open(pthfile, "w") as vf:
    vf.write(r"C:\Users\ein\AppData\Roaming\Autodesk\Revit\Addins\2024")
    vf.write("\n")


import vray
vray.test()