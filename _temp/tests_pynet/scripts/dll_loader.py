import sys
import os.path as op
import clr  # type: ignore

THIS_DIR = op.dirname(op.dirname(__file__))
DLL_NAME = "pynettests.dll"
if sys.platform == "win32":
    DLL_NAME = "pynettests.exe"

clr.AddReference(
    op.normpath(op.join(THIS_DIR, rf"../src/support/PyNetTests/bin/Debug/{DLL_NAME}"))
)
