using System;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
  public abstract class PythonScriptFixture : ScriptFixture
  {
    protected static ILanguage GetPythonInRangeOrSkip(Version min, Version max = default)
    {
      ScriptInfo.IsSkippedByPython skip;

      if (max is null)
        skip = new ScriptInfo.IsSkippedByPython(min);
      else
        skip = new ScriptInfo.IsSkippedByPython(min, max);

      skip.TestSkip(out ILanguage python);
      return python;
    }
  }
}