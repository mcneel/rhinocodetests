import rhinoscriptsyntax as rs
import scriptcontext as sc
from Rhino import RhinoApp


"""
- Run this and hit escape
- Open test_escape.py2
- Run and make sure escape is not sticky
"""

MAX = 1000
rs.StatusBarProgressMeterShow("Progress", 0, MAX)

for i in range(0, MAX):
    rs.StatusBarProgressMeterUpdate(i)
    if sc.escape_test(throw_exception=False): break
    RhinoApp.Wait()

rs.StatusBarProgressMeterHide()