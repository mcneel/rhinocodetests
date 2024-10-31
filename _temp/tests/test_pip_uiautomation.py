# r: uiautomation

import uiautomation as uia

rhino = uia.WindowControl(searchDepth=1, Name='Untitled - Rhino WIP - [Perspective]')
if rhino:
    rhino.SendKeys("RhinoCodeLogs")

print(rhino)