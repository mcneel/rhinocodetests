using System;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public class NotSupportedInGHException : NotSupportedException
  {
    public NotSupportedInGHException()
     : base(
         "This type of object is not supported in Grasshopper, so this Python script cannot create it. " +
         "You might want to use 'scriptcontext.doc = Rhino.RhinoDoc.ActiveDoc' to use the Rhino doc, instead? " +
         "If you do, remember to restore it: 'scriptcontext.doc = ghdoc'.")
    {
    }
  }
}
