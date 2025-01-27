// #! csharp
using System;
using Rhino.Runtime.Code.Text;

string source = @"// Grasshopper Script Instance
//#! csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
  /* 
    Members:
      RhinoDoc RhinoDocument
      GH_Document GrasshopperDocument
      IGH_Component Component
      int Iteration

    Methods (Virtual & overridable):
      Print(string text)
      Print(string format, params object[] args)
      Reflect(object obj)
      Reflect(object obj, string method_name)
  */
  
  private void RunScript(object x, object y, out object a)
  {
    // Write your logic here
    a = null;
  }
}
";

// removing null in a=null assignment
TextPatch patch = new TextPatch(new TextRange(35, 9, 35, 13), string.Empty);
source.TryPatch(patch, out string res);

result = !res.Contains("null");
