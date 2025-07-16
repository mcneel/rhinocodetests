using System;
using System.Text;

using Rhino.Runtime.Code;

namespace RhinoCodePlatform.Rhino3D.Testing
{
  public class RestoreProgressWatcher : ProgressReporter
  {
    protected readonly StringBuilder m_reports = new();

    public bool HasReports { get; protected set; } = false;

    public override void Report(ProgressReport report)
    {
      HasReports = true;
      m_reports.AppendLine(report.Message);
    }

    public bool Contains(string message) => m_reports.ToString().Contains(message);

    public void Reset()
    {
      m_reports.Clear();
      HasReports = false;
    }
  }
}