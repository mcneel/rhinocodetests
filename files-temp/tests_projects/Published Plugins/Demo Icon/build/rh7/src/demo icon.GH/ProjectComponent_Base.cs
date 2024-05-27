using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public abstract class ProjectComponent_Base : GH_Component
  {
    readonly string _script = null;

    public ProjectComponent_Base(string scriptData,
                                 string name,
                                 string nickname,
                                 string description,
                                 string category,
                                 string subCategory)
      : base(name, nickname, description, category, subCategory)
    {
      _script = Encoding.UTF8.GetString(Convert.FromBase64String(scriptData));
    }

    protected bool TryGetScript(out string script)
    {
      if (_script is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed reading component script");
        script = null;
        return false;
      }

      script = _script;
      return true;
    }

    protected static bool TryGetInput(IGH_DataAccess da, int index, GH_ParamAccess access, out object value)
    {
      switch (access)
      {
        case GH_ParamAccess.item:
          object item = null;
          if (da.GetData(index, ref item))
          {
            if (item is IGH_Goo goo)
              item = goo.ScriptVariable();

            value = item;
            return true;
          }
          break;

        case GH_ParamAccess.list:
          var inputList = new List<object>();
          if (da.GetDataList(index, inputList))
          {
            value = inputList;
            return true;
          }

          break;

        case GH_ParamAccess.tree:
          if (da.GetDataTree(index, out GH_Structure<IGH_Goo> inputTree))
          {
            value = inputTree;
            return true;
          }

          break;
      }

      value = default;
      return false;
    }

    protected static void SetOutput(IGH_DataAccess da, int index, object value)
    {
      if (value == null)
        return;

      switch (value)
      {
        case string str:
          da.SetData(index, str);
          break;

        case IGH_Goo ghGoo:
          da.SetData(index, ghGoo);
          break;

        case IGH_Structure ghStruct:
          da.SetDataTree(index, ghStruct);
          break;

        case IGH_DataTree ghDataTree:
          da.SetDataTree(index, ghDataTree);
          break;

        case IEnumerable enumerable:
          da.SetDataList(index, enumerable);
          break;

        case System.Numerics.Complex complex:
          // 6 June 2022 (eirannejad) moved code from ComponentIOMarshal.cs
          // 8 August 2012 (S. Baer) - https://github.com/mcneel/ghpython/issues/17
          // Grasshopper doesn't internally support System.Numerics.Complex right now
          // and uses a built-in complex data structure.  Convert to GH complex when
          // we run into System.Numerics.Complex
          da.SetData(index, new Complex(complex.Real, complex.Imaginary));
          break;

        default:
          da.SetData(index, value);
          break;
      }
    }
  }
}
