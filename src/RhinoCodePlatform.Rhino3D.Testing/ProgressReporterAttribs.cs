using System;
using System.Text;
using System.Text.RegularExpressions;

using RhinoCodePlatform.Rhino3D.GH;
using RhinoCodePlatform.Rhino3D.GH1;

namespace RhinoCodePlatform.Rhino3D.Testing
{
    public sealed class ProgressReporterAttribs
    {
        readonly StringBuilder _messages = new();
        readonly Regex _match;

        public bool Pass => _match.IsMatch(_messages.ToString());

        public ProgressReporterAttribs(IScriptAttribute attribs, Regex match)
        {
            _match = match;

            switch (attribs)
            {
                case GH_ScriptComponentAttributes single:
                    single.Progress += OnSinglePrgress;
                    break;

                case GH_ScriptComponentAttributes_Contextual context:
                    context.Progress += OnContextProgress;
                    break;
            }
        }

        void OnSinglePrgress(GH_ScriptComponentAttributes attribs, float progress, string message)
        {
            _messages.AppendLine(message);
        }

        void OnContextProgress(GH_ScriptComponentAttributes_Contextual attribs, Guid id, float progress, string message)
        {
            _messages.AppendLine(message);
        }
    }
}
