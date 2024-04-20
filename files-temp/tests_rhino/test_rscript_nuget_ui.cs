#r "nuget: FSharp.Core, 7.0.0"
#r "nuget: FsEx, 0.13.1"
#r "nuget: Rhino.Scripting, 0.4.0"

using rs = Rhino.Scripting;


var pt =  rs.GetObject("Select an Object");
rs.ObjectLayer(pt, "some new layer", true);
