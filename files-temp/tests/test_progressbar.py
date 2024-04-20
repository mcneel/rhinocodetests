import Rhino
import scriptcontext as sc
import System

def test_progressbar():
    sn = sc.doc.RuntimeSerialNumber
    min = 0
    max = 100
    Rhino.UI.StatusBar.ShowProgressMeter(sn, min, max, "Starting", True, False)
    for i in range(min, max + 1):
        Rhino.RhinoApp.Wait()
        System.Threading.Thread.Sleep(100)
        if i == 25:
            Rhino.UI.StatusBar.UpdateProgressMeter("Calculating", i, True)
        elif i == 50:
            Rhino.UI.StatusBar.UpdateProgressMeter("Processing", i, True)
        elif i == 75:
            Rhino.UI.StatusBar.UpdateProgressMeter("Finishing", i, True)
        else:
            Rhino.UI.StatusBar.UpdateProgressMeter(i, True)
    System.Threading.Thread.Sleep(1000)
    Rhino.UI.StatusBar.HideProgressMeter(sn)

test_progressbar()
