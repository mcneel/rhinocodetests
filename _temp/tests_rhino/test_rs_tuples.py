#! python 3

import rhinoscriptsyntax as rs
import scriptcontext as sc

import System
import System.Collections.Generic
import Rhino

p = rs.CreatePoint(1, 2, 3)
print(len(p))
print([p[0], p[1], p[2]])
print([c for c in iter(p)])
print(list(p))

p = rs.CreateVector(1, 2, 3)
print(len(p))
print([p[0], p[1], p[2]])
print([c for c in iter(p)])
print(list(p))

p = rs.CreatePoint(1, 2, 3) + rs.CreateVector(3, 2, 1)
print(len(p))
print([p[0], p[1], p[2]])
print([c for c in iter(p)])
print(list(p))
