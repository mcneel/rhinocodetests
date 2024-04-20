#! python 3
import rhinoscriptsyntax as rs
import scriptcontext as sc

import decimal
    
import System
import System.Collections.Generic
import Rhino

idSurface = rs.GetObject("Surface to frame", 8, True, True)

intCount = rs.GetInteger("Number of iterations per direction", 20, 2)

uDomain = rs.SurfaceDomain(idSurface, 0)
vDomain = rs.SurfaceDomain(idSurface, 1)
uStep = (uDomain[1] - uDomain[0]) / intCount
vStep = (vDomain[1] - vDomain[0]) / intCount


def float_range(start, stop, step):
    start = int(start)
    stop = int(stop)
    while start < stop:
        yield float(start)
        start += decimal.Decimal(step)


rs.EnableRedraw(False)
for u in float_range(uDomain[0],uDomain[1], uStep):
    for v in float_range(vDomain[0],vDomain[1],vStep):
        pt = rs.EvaluateSurface(idSurface, u, v)
        if rs.Distance(pt, rs.BrepClosestPoint(idSurface, pt)[0]) < 0.1:
            srfFrame = rs.SurfaceFrame(idSurface, [u, v])
            rs.AddPlaneSurface(srfFrame, 1.0, 1.0)

rs.EnableRedraw(True)
