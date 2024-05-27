using System;
using System.IO;
using System.Text;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin
{
  static class ProjectLibs
  {
    const string LIBS_DIR_NAME = "libs";
    static bool s_pythonPathInitd = false;

    public static string GetPluginPath() => Path.GetDirectoryName(typeof(ProjectLibs).Assembly.Location);

    public static void InitPythonLibraries()
    {
      if (s_pythonPathInitd)
        return;

      var script = new StringBuilder("import sys\n");
      string libpath = Path.Combine(GetPluginPath(), LIBS_DIR_NAME);
      foreach (var zipfile in Directory.GetFiles(libpath, "*.zip"))
        script.AppendLine($"sys.path.append(r\"{zipfile}\")");

      var s = Rhino.Runtime.PythonScript.Create();
      s.Compile(script.ToString()).Execute(s);

      s_pythonPathInitd = true;
    }
  }
}
