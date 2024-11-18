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
    public sealed class ProgressWatcher
    {
        readonly StringBuilder _report = new();
        readonly Regex _match;

        public bool Pass => _match.IsMatch(GetReport());

        public string GetReport() => _report.ToString();

        public ProgressWatcher(IScriptAttribute attribs, Regex match)
        {
            _match = match;

            switch (attribs)
            {
                case GH_ScriptComponentAttributes single:
                    single.Progress += OnSinglePrgress;
                    return;

                case GH_ScriptComponentAttributes_Contextual context:
                    context.Progress += OnContextProgress;
                    return;
            }

            string type = attribs?.GetType().FullName ?? "null";
            throw new Exception($"Unknown {nameof(IScriptAttribute)} type: {type}");
        }

        void OnSinglePrgress(GH_ScriptComponentAttributes attribs, float progress, string message)
        {
            _report.AppendLine(message);
        }

        void OnContextProgress(GH_ScriptComponentAttributes_Contextual attribs, Guid id, float progress, string message)
        {
            _report.AppendLine(message);
        }
    }
}
