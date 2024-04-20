#! python3

import rhinoscriptsyntax as rs
import scriptcontext as sc
import math

import System
import System.Collections.Generic
import Rhino


ids = rs.ObjectsByType(geometry_type=1, select=False, state=1)

assert len(ids) == 5