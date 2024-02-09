using System;

using Rhino.Runtime.Code.Storage;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public sealed class ScriptInfo
    {
        public Uri Uri { get; }

        public bool IsDebug { get; }

        public bool ExpectsError { get; }

        public ScriptInfo(Uri scriptPath)
        {
            if (scriptPath is null)
                throw new ArgumentNullException(nameof(scriptPath));

            string uriStr = scriptPath.ToString().ToLower();

            Uri = scriptPath;
            IsDebug = uriStr.Contains("_debug");
            ExpectsError = uriStr.Contains("_error");
        }

        public override string ToString() => Uri.GetEndpointTitle();
    }
}
