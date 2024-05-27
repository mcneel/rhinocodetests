using System;
using System.Text;

using Rhino.Geometry;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public sealed class ProjectComponent_Python_ca23727f : ProjectComponent_Base
  {
    static readonly ProxyDocument s_document = new ProxyDocument();

    Rhino.Runtime.PythonScript _script;
    Rhino.Runtime.PythonCompiledCode _compiledCode;

    public override Guid ComponentGuid { get; } = new Guid("ca23727f-1fed-497d-a77b-e213cea8ef20");

    public ProjectComponent_Python_ca23727f() : base(
        scriptData: "YSA9IHggKyB5DQo=",
        name: "Add",
        nickname: "Add",
        description: "",
        category: "demo icon",
        subCategory: "Default"
        )
    {
      ProjectLibs.InitPythonLibraries();
    }

    protected override void RegisterInputParams(GH_InputParamManager pm)
    {
      			pm.AddParameter(new Param_Integer(), "x", "x", "Converts to collection of integer numbers", (GH_ParamAccess)0);
			pm.AddParameter(new Param_Integer(), "y", "y", "Converts to collection of integer numbers", (GH_ParamAccess)0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pm)
    {
      			pm.AddParameter(new Param_GenericObject(), "a", "a", "No conversion", (GH_ParamAccess)0);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (_compiledCode is null)
      {
        if (TryGetSource(out string script))
        {
          try
          {
            _script = Rhino.Runtime.PythonScript.Create();
            _compiledCode = _script.Compile(script);
          }
          catch (Exception ex)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to compile script | {ex}");
          }
        }
      }

      _script.ScriptContextDoc = s_document;
      _script.SetVariable("ghdoc", s_document);
      _script.SetVariable("ghenv", new ScriptEnv(DA, this, s_document));
      _script.SetVariable("__name__", "__main__");

      s_document.Component = this;

      int index = 0;
      foreach (IGH_Param input in Params.Input)
      {
        if (TryGetInput(DA, index, input.Access, out object value))
        {
          if (value is IGH_Goo goo)
            value = goo.ScriptVariable();

          _script.SetVariable(input.Name, value);
        }
        else
          _script.SetVariable(input.Name, null);
        index++;
      }

      _compiledCode.Execute(_script);

      index = 0;
      foreach (IGH_Param output in Params.Output)
      {
        SetOutput(DA, index, _script.GetVariable(output.Name));
        index++;
      }
    }

    bool TryGetSource(out string script)
    {
      if (!TryGetScript(out script))
        return false;

      var source = new StringBuilder();

      source.Append(@"
import scriptcontext

def _get_active_component():
    if scriptcontext.doc:
        return getattr(scriptcontext.doc, 'Component', None)

__builtins__['ghenv'] = ghenv
scriptcontext.doc = ghenv.LegacyDocument

import ghpythonlib
__ironpython_original_component_getter__ = ghpythonlib.component._get_active_component
ghpythonlib.component._get_active_component = _get_active_component
");

      source.Append(script);

      source.Append(@"
scriptcontext.doc = None
ghpythonlib.component._get_active_component = __ironpython_original_component_getter__
");

      script = source.ToString();
      return true;
    }
  }
}
