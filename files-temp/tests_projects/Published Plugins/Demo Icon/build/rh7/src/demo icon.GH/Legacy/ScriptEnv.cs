using System;
using System.Reflection;

using Grasshopper.Kernel;

namespace RhinoCodePlatform.Rhino3D.Projects.Plugin.GH
{
  public class ScriptEnv
  {
    public IGH_Component Component { get; }

    public GH_Document Document { get; }

    public ProxyDocument LegacyDocument { get; }

    public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

    public IGH_DataAccess DataAccessManager { get; }

    public ScriptEnv(IGH_DataAccess da, IGH_Component component, ProxyDocument proxyDoc)
    {
      Component = component;
      Document = component.OnPingDocument();
      LegacyDocument = proxyDoc;
      DataAccessManager = da;
    }
  }
}
