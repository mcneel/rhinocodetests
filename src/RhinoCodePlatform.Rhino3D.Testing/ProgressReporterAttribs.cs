using System;
using System.Text;
using System.Text.RegularExpressions;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

using RhinoCodePlatform.Rhino3D.GH;

namespace RhinoCodePlatform.Rhino3D.Testing
{
    public sealed class ProgressReporterAttribs : GH_ComponentAttributes, IScriptAttribute
    {
        readonly StringBuilder _messages = new();
        readonly Regex _match;

        public bool Pass => _match.IsMatch(_messages.ToString());

        public ProgressReporterAttribs(IGH_Component component, Regex match) : base(component)
        {
            _match = match;
        }

        public void ShowProgress(float progress, string message)
        {
            _messages.AppendLine(message);
        }
    }
}
