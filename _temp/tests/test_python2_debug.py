"""
Note:
This is an IronPython script.
Autocompletion, Diagnostics and Debugging have not been
implemented yet.
"""
#! python 2

import sys
import os.path
import pprint
# import rhinoscriptsyntax as rs
import scriptcontext as sc

import Rhino

pt = Rhino.Geometry.Point3d(1,2,3)

def test_debug(m, l, d):
    print("{0}".format(m))
    pprint.pprint(l)
    pprint.pprint(d)

print
print(sys.version)
test_debug(2, [1,2,3,4], {"ehsan":12, "dsds":42})