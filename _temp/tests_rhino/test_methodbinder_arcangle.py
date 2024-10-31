#! python 3

import rhinoscriptsyntax as rs

arc = rs.GetObject("Select an arc")
print(rs.ArcAngle(arc))
