#! python3
# https://mcneel.myjetbrains.com/youtrack/issue/RH-80211
# `with` on a .NET IDisposable should call Dispose() on exit

import System
import Rhino

# plain .NET IDisposable
with System.IO.StringWriter() as w:
    w.Write("hello")

writerDisposed = False
try:
    w.Write("x")
except System.ObjectDisposedException:
    writerDisposed = True

# RhinoCommon IDisposable; a disposed doc can no longer be found by its serial number
with Rhino.RhinoDoc.CreateHeadless(None) as doc:
    doc.Objects.AddPoint(Rhino.Geometry.Point3d.Origin)
    sn = doc.RuntimeSerialNumber

docDisposed = Rhino.RhinoDoc.FromRuntimeSerialNumber(sn) is None

result = writerDisposed and docDisposed
