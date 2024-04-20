#! python 2

import sys

import rhinoscriptsyntax as rs

print(sys.version)
print('\n'.join(sys.path))
print(rs)

try:
    import testipymodule as tp
    print(tp.__file__)
    print(tp.riazi.add(21, 21))
except Exception as e:
    print "Error loading testipymodule"
    print str(e)