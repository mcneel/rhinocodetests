using System;
using System.Text.RegularExpressions;

using Rhino.Runtime.Code.Storage;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public sealed class ScriptInfo
    {
        static readonly Regex s_errorMatcher = new(@"_error\((?<message>.+)\)");
        
        public Uri Uri { get; }

        public bool IsDebug { get; } = false;

        public bool ExpectsError { get; } = false;
        
        public string ExpectsErrorMessage { get; }

        public bool IsSkipped { get; } = false;

        public ScriptInfo(Uri scriptPath)
        {
            if (scriptPath is null)
                throw new ArgumentNullException(nameof(scriptPath));

            string uriStr = scriptPath.ToString().ToLower();

            Uri = scriptPath;
            IsDebug = uriStr.Contains("_debug");

            ExpectsError = uriStr.Contains("_error");
            Match m = s_errorMatcher.Match(uriStr);
            if (m.Success)
            {
                ExpectsErrorMessage = m.Groups["message"].Value;
            }

            IsSkipped = uriStr.Contains("_skip");
        }

        public override string ToString() => Uri.GetEndpointTitle();
    }
}
