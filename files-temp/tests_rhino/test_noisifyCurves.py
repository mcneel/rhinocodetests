#! python 3
import rhinoscriptsyntax as rs
import scriptcontext as sc
import Rhino
import random

from System.Collections.Generic import List

class DrawCurvesConduit(Rhino.Display.DisplayConduit):
    # class members
    
    # class constructor
    def __init__(self, crvs):
        super().__init__()
        self.crvs = [crv.ToNurbsCurve() for crv in crvs]
        self.draw_color = Rhino.ApplicationSettings.AppearanceSettings.TrackingColor
        self.bbox = Rhino.Geometry.BoundingBox(0, 0, 0, 0, 0, 0)
        for crv in crvs:
            self.bbox.Union(crv.ToNurbsCurve().GetBoundingBox(True))
        
    def CalculateBoundingBox(self, e):
        e.IncludeBoundingBox(self.bbox)
        
    # DrawOverlay override
    def DrawOverlay(self, e):
        for crv in self.crvs:
            e.Display.DrawCurve(crv, self.draw_color, 3)


def CalculateCurves(ids, space, scale, crvType, planar, noiseType):
    
    size = scale*space*.5
    crvList = ["Soft", "Medium", "Hard"]
    crvs = [rs.coercecurve(id) for id in ids]
    result = []
    
    def RandomizePt(pt,crv, size, noiseType, plane):
        pass
        coords = []
        vecDir = crv.FrameAt(crv.ClosestPoint(pt)[1])[1].YAxis
        pass
        vecScale = random.uniform(-size, size)
        vecDir *= vecScale
        
        targ = pt + vecDir
        return targ

    crvsOut = []
    for crv in crvs:
        
        diag = crv.GetBoundingBox(True).Diagonal
        pass 
        
        crntPlane = None
        pars = list(crv.DivideByLength(space, True))
        if not crv.IsClosed:
            pars.append(crv.Domain.Max)
        pts = [crv.PointAt(par) for par in pars]
        newPts = []
        
        if crv.IsPlanar():
            if planar:
                rc, crntPlane = crv.TryGetPlane()
            else:
                crntPlane = rs.ViewCPlane()
        if not crv.IsClosed:
            for pt in pts:
                newPts.append(RandomizePt(pt, crv, size, noiseType, crntPlane))
            #newPtsappend(pts[len(pts)-1])
        else:
            for n in range(len(pts)-1):
                newPts.append(RandomizePt(pts[n],crv, size, noiseType, crntPlane))
                
            #newPts.append(pts[len(pts)]-1)
            newPts.append(newPts[0])
        if crvType == 0:
            geometry = List[Rhino.Geometry.Point3d]()
            for pt in newPts:
                geometry.Add(Rhino.Geometry.Point3d(pt))
            temp = Rhino.Geometry.Curve.CreateInterpolatedCurve(geometry, 3, Rhino.Geometry.CurveKnotStyle.Uniform)
        else:
            temp = Rhino.Geometry.Polyline(newPts)
        crvsOut.append(temp)
    return crvsOut
    pass

def NoisifyCurves():
    
    ids = rs.GetObjects("Select curves to noisify.", 4, preselect=True, select=False)
    if not ids: return
    
    rs.UnselectAllObjects()
    
    while True:
        space=1.0
        scale = 3
        crvType = 1
        planar=True
        noiseType = 0
        delIn = False
        
        
        if "NOISE_SPACE"in sc.sticky :
            space = sc.sticky["NOISE_SPACE" ]
            
        if "NOISE_SCALE" in sc.sticky:
            scale = sc.sticky["NOISE_SCALE"]
            
        if "NOISE_CRV"in sc.sticky:
            crvType = sc.sticky["NOISE_CRV"]
            
        if "NOISE_PLANAR"in sc.sticky:
            planar = sc.sticky["NOISE_PLANAR"]
            
        if "NOISE_NOISE"in sc.sticky:
            noiseType = sc.sticky["NOISE_NOISE"]
            
            
        if "NOISE_DELETEINPUT"in sc.sticky:
            delIn = sc.sticky["NOISE_DELETEINPUT"]
            
            
        crvs = CalculateCurves(ids, space, scale, crvType, planar, noiseType)
        if not crvs: return
    #        for crv in crvs:
    #            sc.doc.Objects.AddPolyline(crv)
        conduit = DrawCurvesConduit(crvs)
        conduit.Enabled = True
        sc.doc.Views.Redraw()
            
        #return
        go = Rhino.Input.Custom.GetOption()
        go.SetCommandPrompt("Set option. Press Enter to accept.")
        go.AcceptNumber(True, False)
        go.AcceptNothing(True)

        crvList = ["Soft",  "Hard"]
        
        opScale = Rhino.Input.Custom.OptionDouble(scale,.1,10)
        opSpace = Rhino.Input.Custom.OptionDouble(space)
        opType = Rhino.Input.Custom.OptionToggle(bool(noiseType), "3D", "2D")
        opPlanar = Rhino.Input.Custom.OptionToggle(planar, "No", "Yes")
        opDel = Rhino.Input.Custom.OptionToggle(delIn, "No", "Yes")
        
        go.AddOptionDouble("Spacing",opSpace)
        go.AddOptionDouble("Scale",opScale)
        go.AddOptionList("Style", crvList, crvType)
        go.AddOptionToggle("InCurvePlane", opPlanar)
        go.AddOptionToggle("NoiseType", opType)
        go.AddOptionToggle("DeleteInput", opDel)
    
        ret = go.Get()
        
        pass
        if go.CommandResult()!=Rhino.Commands.Result.Success:
            conduit.Enabled=False
            
            break
            
        elif ret ==Rhino.Input.GetResult.Option:
            idx = go.OptionIndex()
            print(idx)
            if idx == 1:
                 space = opSpace.CurrentValue
                 sc.sticky["NOISE_SPACE"] = space
            elif idx ==2:
                scale = opScale.CurrentValue
                sc.sticky["NOISE_SCALE"] = scale
            elif idx == 3:
                crvType = go.Option().CurrentListOptionIndex
                sc.sticky["NOISE_CRV"] = crvType
            elif idx == 4:
                planar = opPlanar.CurrentValue
                sc.sticky["NOISE_PLANAR"] = planar
                
            elif idx == 5:
                noiseType = opType.CurrentValue
                sc.sticky["NOISE_NOISE"] = noiseType
            elif idx == 6:
                delIn = opDel.CurrentValue
                sc.sticky["NOISE_DELETEINPUT"] = delIn
            conduit.Enabled= False
            continue
        elif ret == Rhino.Input.GetResult.Nothing:
            conduit.Enabled = False
            sc.doc.Views.Redraw()
            break
        elif ret == Rhino.Input.GetResult.Number:
            num = go.Number()
            if 1 <= num <=10:
                scale = num
            sc.sticky["NOISE_SCALE"] = scale
            conduit.Enabled = False
            continue

    conduit.Enabled = False
    sc.doc.Views.Redraw()
    
    newIds = []
    
    print('type = ' + str(crvType))
    if crvs:
        for crv in crvs:
            if crvType == 0:
                newIds.append(sc.doc.Objects.AddCurve(crv))
            else:
                newIds.append(sc.doc.Objects.AddPolyline(crv))
                
        if delIn: rs.DeleteObjects(ids)
        
        if len(newIds) >0:
            rs.UnselectAllObjects()
            rs.SelectObjects(newIds)
            
    sc.doc.Views.Redraw()
    pass

if __name__=="__main__": NoisifyCurves()