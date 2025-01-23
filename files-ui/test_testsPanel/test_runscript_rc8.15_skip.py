#! python3
# https://mcneel.myjetbrains.com/youtrack/issue/RH-85032
# Awaiting fix RH-85032

import os.path as op
from Rhino import RhinoApp
from Rhino import RhinoDoc

runScript = op.join(op.dirname(__file__), r'rhino\test_runScript.py')
result = RhinoApp.RunScript(f"-_ScriptEditor _Run \"{runScript}\"", True)