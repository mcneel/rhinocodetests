import Rhino
import scriptcontext as sc
import rhinoscriptsyntax as rs

#import a couple of things from System
from System.Collections.Generic import IEnumerable
from System import Double

tol = .001
targetId = rs.GetObject("Select surface or polyusurface to split", filter=8+16, preselect=True)
targetBrep = rs.coercebrep(targetId)

cuttingIds = rs.GetObjects("Select cutting surfaces or polysurfaces", filter=8+16)
cuttingBreps = [rs.coercebrep(id) for id in cuttingIds]

# When calling the splitter, you need to first tell Python what type of object you are going to 
# give it for each parameter and then provide the actual parameter values:
# In this case the method needs a list of either breps or curves and a tolerance
#the code specifies the two types ahead of time - Rhino.Geometry.Brep and a number, System.Double, here as 'sd'
# imported at the top of the script. 
splitList = targetBrep.Split.Overloads[IEnumerable[Rhino.Geometry.Brep], Double]( cuttingBreps, tol)


newIds = [ sc.doc.Objects.AddBrep(brep) for brep in splitList]