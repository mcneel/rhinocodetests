using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;

var go = new GetObject();
go.GeometryFilter = Rhino.DocObjects.ObjectType.Surface | Rhino.DocObjects.ObjectType.Curve;
go.GeometryAttributeFilter = GeometryAttributeFilter.MatedEdge | GeometryAttributeFilter.EdgeCurve;
go.SetCommandPrompt("Select edge or face");
go.AcceptNothing(true);
go.DisablePreSelect();
go.EnableHighlight(false);
go.ChooseOneQuestion = true;
go.BottomObjectPreference = true;
var getResult = go.GetMultiple(1, -1);

foreach (var o in go.Objects())
{
  RhinoApp.WriteLine("Selected {0}", o.Geometry().GetType().ToString());
}