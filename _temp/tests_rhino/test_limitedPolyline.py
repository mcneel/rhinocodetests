import Rhino
import scriptcontext as sc
import System

def LimitedPolyline():
    tol = sc.doc.ModelAbsoluteTolerance
    
    lineColor = sc.doc.Layers.CurrentLayer.Color
    targColor = System.Drawing.Color.Red
    ghostColor = Rhino.ApplicationSettings.AppearanceSettings.TrackingColor
    ghostColor = System.Drawing.Color.FromArgb(128, ghostColor.R, ghostColor.G, ghostColor.B)

    Xstyle = Rhino.Display.PointStyle.X
    Ostyle = Rhino.Display.PointStyle.RoundSimple
    
    targetLength = 100
    crntLength = 0
    
    pts = []
    KEY = "TARGET_LENGHTH"
    constrain = None
    def GetPointDynamicDrawFunc( sender, args ):
        crntPt = args.CurrentPoint
        lastPt = pts[len(pts)-1]
        args.Display.DrawPoint(crntPt)

        vecDir = crntPt - lastPt
        vecDir.Unitize()
        vecDir *= targetLength - crntLength
        vecNow = crntPt - lastPt

        if vecNow.Length > targetLength - crntLength:
            drawPt = lastPt + vecDir
        else:
            drawPt = crntPt
            
        args.Display.DrawLine(lastPt, drawPt, lineColor, 2)
        args.Display.DrawLine(lastPt + vecDir, crntPt, ghostColor, 2)
        args.Display.DrawPolyline(pts,lineColor, 2)

        args.Display.DrawPoint(lastPt + vecDir, Ostyle,3, targColor)

        for pt in pts:
            args.Display.DrawPoint(pt, Xstyle, 5, lineColor)
        
    while crntLength < targetLength:
        targetLength = 100
        if KEY in sc.sticky:
            targetLength= sc.sticky[KEY]
            
        gp = Rhino.Input.Custom.GetPoint()
        

        opLen = Rhino.Input.Custom.OptionDouble(targetLength)
        gp.AddOptionDouble("Length",opLen)
        gp.AcceptNumber(True, False)
        gp.AcceptPoint(True)
        
        if len(pts) >0:
            bPt = pts[len(pts)-1]
            gp.SetBasePoint(bPt, True)
            if constrain is not None:
               gp.ConstrainDistanceFromBasePoint( constrain)
               constrain = None
               
            gp.DynamicDraw += GetPointDynamicDrawFunc
            
            gp.AcceptNothing(True)
            
            print ("Remaining " + str(round(targetLength-crntLength, 3)))
            for pt in pts:
                gp.AddSnapPoint(pt) 

        rc = gp.Get()
    
        if gp.CommandResult() != Rhino.Commands.Result.Success:
            break
        if rc ==Rhino.Input.GetResult.Option:
            idx = gp.OptionIndex()
            targetLength = opLen.CurrentValue
            sc.sticky[KEY] = targetLength
            continue
            
        elif  rc ==Rhino.Input.GetResult.Number:
            constrain = abs(gp.Number())
            continue

        elif  rc ==Rhino.Input.GetResult.Nothing:
            break

        elif rc ==Rhino.Input.GetResult.Point:
            pt = gp.Point()
            
            if len(pts) > 1:
                if pt.DistanceTo(pts[0]) < tol:
                    pts.append(pts[0])
                    break
            if len(pts) >0:
                increment = pt.DistanceTo(pts[len(pts)-1])
                
                if increment > targetLength-crntLength:
                    increment = targetLength-crntLength
                    vecDir = pt - pts[len(pts)-1]
                    vecDir.Unitize()
                    vecDir *= increment
                    pt = pts[len(pts)-1] + vecDir
                    
                crntLength += increment
                print("Current " + str(round(crntLength, 3)))
            pts.append(pt)
                 
    if len(pts) > 1:
        pLine = Rhino.Geometry.Polyline(pts)
        sc.doc.Objects.AddPolyline(pts)
        print("Final length " + str(pLine.Length))
        sc.doc.Views.Redraw()
        
if __name__ == '__main__':
    LimitedPolyline()