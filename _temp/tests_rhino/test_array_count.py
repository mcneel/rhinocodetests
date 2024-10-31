"""Test Array .Count property

Monkey-patched in RhinoCodePlatform.Rhino3D\Resources\mcneelpy3_prep.py

- Draw a plane
- Run script
- Select a plane edge near a corner
"""
import rhinoscriptsyntax as rs
import Rhino
import scriptcontext as sc
import math
from System.Collections.Generic import IEnumerable
from System import Double as sd

    
def FilletSrfCorner():
    
    tol = sc.doc.ModelAbsoluteTolerance
    pi = math.pi
    

    def RebuildArc(arc):
        
        deg = arc.AngleDegrees
        
        if deg <=45:
            rebuild = [3,4]
        elif 70 >= deg > 45:
            rebuild = [4,5]
        elif 90 >= deg > 70:
            rebuild = [5,6]
        else:
            rebuild = [5, int(deg/15)]
        x = arc.Rebuild(rebuild[1], rebuild[0], True)
        return arc.Rebuild(rebuild[1], rebuild[0], True)
    
    planes = [None, rs.ViewCPlane(), Rhino.Geometry.Plane.WorldXY]
    
    def GetInputs(count):

        planeList = ['Auto','CPlane','WorldXY']
        
        while True:
            #defaults
            rad = 1
            if sc.sticky.has_key('NP_RAD'):
                rad = sc.sticky['NP_RAD']
            
            planeIdx = 0
            if sc.sticky.has_key('FT_PLANE_IDX'):
                planeIdx = sc.sticky['FT_PLANE_IDX']
            else:
                sc.sticky['FT_PLANE_IDX'] = planeIdx
            go = Rhino.Input.Custom.GetObject()
            promptString = "Select an edge near the corner to fillet"
            go.SetCommandPrompt(promptString)
            
            #options
            opRad = Rhino.Input.Custom.OptionDouble(rad)
            go.AddOptionDouble("Radius", opRad)
            
            go.AddOptionList("Plane", planeList, planeIdx)
            
            if count > 0:
                go.AddOption("Undo")
            
            go.AcceptNumber(True, False)
            go.GeometryFilter = Rhino.DocObjects.ObjectType.EdgeFilter
            go.EnablePreSelect(False, True)
            
            rc = go.Get()
            
            if go.CommandResult()!=Rhino.Commands.Result.Success:
                return go.CommandResult()
                
            if rc==Rhino.Input.GetResult.Object:
                objRef=go.Object(0)
                return objRef, rad
                
            elif rc==Rhino.Input.GetResult.Number:
                rad = go.Number()
                sc.sticky['NP_RAD']= rad
                continue
                
            elif rc==Rhino.Input.GetResult.Option:
                idx = go.OptionIndex()
                if idx == 1:
                    rad = opRad.CurrentValue
                    sc.sticky['NP_RAD']= rad
                elif idx == 2:
                    planeIdx = go.Option().CurrentListOptionIndex
                    sc.sticky['FT_PLANE_IDX'] = planeIdx
                else:
                    sc.doc.Objects.Replace(srfId,brep)
                    count = 0
                    sc.doc.Views.Redraw()
                continue
    count = 0
    while True:
        #brep = rs.coercebrep(srfId)

        inputs = GetInputs(count)
        if inputs == Rhino.Commands.Result.Cancel: return
        count += 1
        objRef, rad = inputs
        srfId = objRef.ObjectId
        brep = objRef.Brep()
        verts = brep.Vertices
        vLocs = [vert.Location for vert in verts]
        par1 = objRef.CurveParameter()[1]
        selPt = objRef.SelectionPoint()
        idx = rs.PointArrayClosestPoint(vLocs,selPt)
        vert = verts[idx]
        pt = vert.Location
        indices = vert.EdgeIndices()
        indices_count = indices.Count
        crvs = [brep.Edges[vert.EdgeIndices()[n]].EdgeCurve for n in range(vert.EdgeIndices().Count)]
        pass
        #sub1, sub2, pts = SortCurveEnds(edges)
            
        crv1 =crvs[0]
        crv2 = crvs[1]
        
        o = vert.Location
        
        par1 = crv1.ClosestPoint(o)[1]
        par2 = crv2.ClosestPoint(o)[1]
        crvTan1 = crv1.TangentAt(par1)
        crvTan2 = crv2.TangentAt(par2)
        
        planeIdx = sc.sticky['FT_PLANE_IDX']
        if planeIdx ==0:
            myPlane = Rhino.Geometry.Plane(o, crvTan1, crvTan2)
        else:
            myPlane = planes[planeIdx]
            
        pull1 = Rhino.Geometry.Curve.ProjectToPlane(crv1,myPlane)
        pull2 = Rhino.Geometry.Curve.ProjectToPlane(crv2,myPlane)
    
        ft = Rhino.Geometry.Curve.CreateFillet(pull1, pull2, rad, par1, par2)
        if not ft: return
        
        if brep.Faces[0].IsPlanar():
            crv = Rhino.Geometry.ArcCurve(ft)
        else:
            crv = RebuildArc(Rhino.Geometry.ArcCurve(ft))
            cPts = crv.Points
        
            # get plane normal lines from the ends of the fillets and
            # intersect with the input curves to find where to place the
            # rebuilt fillet ends and tangent directions
            bb = crv1.GetBoundingBox(False)
            
            bb.Union(crv2.GetBoundingBox(False))
            norm = Rhino.Geometry.Line(crv.PointAtStart,crv.PointAtStart + myPlane.ZAxis)
            norm.ExtendThroughBox(bb)
            
            if norm.Length == 0:
                tanPt1 = crv.PointAtStart
            else:
                norm = Rhino.Geometry.LineCurve(norm)
                intPt = Rhino.Geometry.Intersect.Intersection.CurveCurve(crv1, norm, tol, tol)
                tanPt1 = intPt[0].PointA
            
            tanPar1 = crv1.ClosestPoint(tanPt1)[1]
            tan1 = crv1.TangentAt(tanPar1)
            
            norm = Rhino.Geometry.Line(crv.PointAtEnd,crv.PointAtEnd + myPlane.ZAxis)
            norm.ExtendThroughBox(bb)
            if norm.Length == 0:
                tanPt2 = crv.PointAtEnd
            else:
                norm = Rhino.Geometry.LineCurve(norm)
                intPt = Rhino.Geometry.Intersect.Intersection.CurveCurve(crv2, norm, tol, tol)
                tanPt2 = intPt[0].PointA
            tanPar2 = crv2.ClosestPoint(tanPt2)[1]
            tan2 = crv2.TangentAt(tanPar2)
            
            # decide which way the tangents should go.
            
            tanStart = cPts[1].Location-cPts[0].Location
            tanEnd = cPts[cPts.Count-2].Location - cPts[cPts.Count-1].Location
            
            if Rhino.Geometry.Vector3d.VectorAngle(tanStart,tan1) > pi/2:
                tan1.Reverse()
                
            if Rhino.Geometry.Vector3d.VectorAngle(tanEnd,tan2) > pi/2:
                tan2.Reverse()
        
            rc, cp1 = crv1.ClosestPoint(crv.PointAtStart)
            rc, cp2 = crv2.ClosestPoint(crv.PointAtEnd)
            
            d1 = cPts[0].Location.DistanceTo(cPts[1].Location)
            d2 = cPts[cPts.Count-1].Location.DistanceTo(cPts[cPts.Count-2].Location)
            tan1 = tan1*d1
            tan2 = tan2*d2
            
            cPts.SetPoint(0, crv1.PointAt(cp1))
            cPts.SetPoint(1, crv1.PointAt(cp1) + tan1)
            cPts.SetPoint(cPts.Count-1, crv2.PointAt(cp2))
            cPts.SetPoint(cPts.Count-2, crv2.PointAt(cp2)+ tan2)
            
            if cPts.Count > 4:
                #move the plane to the second cp location:
                myPlane.Origin = cPts[cPts.Count-2].Location
                
                #find the direction vector and unitize it
                vecDir =  myPlane.ClosestPoint(cPts[1].Location) - cPts[1].Location
                #vecDir.Unitize()
                
                vecDir.Reverse()
                
                #get curve point locations in plane coorinates
                xform = Rhino.Geometry.Transform.PlaneToPlane( myPlane,Rhino.Geometry.Plane.WorldXY )
                locs = [xform*cPt.Location  for cPt in cPts]
                
                #get the locations as projected to the plane 
                planePts = [myPlane.ClosestPoint(cPt.Location) for cPt in cPts]
                
                #get the fillet curve params for the tangent points
                crvDom = crv.Domain
                ftPar1 = crv.ClosestPoint(cPts[1].Location)[1]
                ftPar2 = crv.ClosestPoint(cPts[cPts.Count-2].Location)[1]
                
                #make a domain between the tangent points
                domPts = Rhino.Geometry.Interval(ftPar1, ftPar2)
                
                for i in range(2,cPts.Count-2):
                    ptPar = crv.ClosestPoint(cPts[i].Location)[1]
                    # ptPar = ptPar - crvDom.Min #<<< par in domPts
                
                    # get the normalized param of the point in 
                    # the sub-domain
                    nPar = domPts.NormalizedParameterAt(ptPar)
                    
                    z = (math.cos(nPar * pi)+1)/2
                    targ = planePts[i]+vecDir*z
                    cPts.SetPoint(i, targ)

        splitter = crv.PullToBrepFace(brep.Faces[0], tol)[0]
     
        breps = brep.Split.Overloads[IEnumerable[Rhino.Geometry.Curve], sd]( [splitter], tol)
    
        if breps:
            for item in breps:
                if item is not None:
                    test = item.Faces[0].PullPointsToFace([pt], tol)
                    if len(test) == 0:
                        sc.doc.Objects.Replace(objRef.ObjectId,item)
                        sc.doc.Views.Redraw()
                        break
    
if __name__== '__main__': FilletSrfCorner()