using System;
using System.Text;
using System.Text.RegularExpressions;

#if RC8_11
using RhinoCodePlatform.GH.Context;
using RhinoCodePlatform.Rhino3D.Languages.GH1.Attributes;
#else
using RhinoCodePlatform.Rhino3D.GH;
using RhinoCodePlatform.Rhino3D.GH1;
#endif

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
