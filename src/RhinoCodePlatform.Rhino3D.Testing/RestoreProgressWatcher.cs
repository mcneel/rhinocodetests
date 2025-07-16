using System;

using Rhino.Runtime.Code;

namespace RhinoCodePlatform.Rhino3D.Testing
{
  public class RestoreProgressWatcher : ProgressReporter
  {
    public bool HasReports { get; protected set; } = false;

    public override void Report(ProgressReport report)
    {
      HasReports = true;
      base.Report(report);
    }

    public void Reset() => HasReports = false;
  }
}