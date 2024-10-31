#r "nuget: FsEx, 0.13.1"
#r "nuget: FSharp.Core, 7.0.0"
#r "nuget: Rhino.Scripting, 0.3.0"

using rs = Rhino.Scripting;

var pt =  rs.AddPoint(2.0, 2.0, 2.0);
rs.ObjectLayer(pt, "some new layer", true);