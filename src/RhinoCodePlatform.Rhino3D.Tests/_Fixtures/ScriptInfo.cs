#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
#pragma warning disable IDE0090 // Use 'new(...)'
using System;
using System.IO;
using System.Text.RegularExpressions;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Storage;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public sealed class ScriptInfo
    {
        static readonly Regex s_rhinoVersionFinder = new Regex(@"(rc|rh|gh)(?<major>\d)\.(?<minor>\d)");

        public Uri Uri { get; }

        public string Name { get; }

        public bool IsDebug { get; } = false;

        public bool IsSkipped { get; } = false;

        public bool ExpectsError { get; } = false;

        public ScriptInfo(Uri scriptPath)
        {
            if (scriptPath is null)
                throw new ArgumentNullException(nameof(scriptPath));

            string uriStr = scriptPath.ToString().ToLower();

            Uri = scriptPath;
            Name = Uri.GetEndpointTitle();
            IsDebug = uriStr.Contains("_debug");
            IsSkipped = uriStr.Contains("_skip");
            ExpectsError = uriStr.Contains("_error");

            Match m = s_rhinoVersionFinder.Match(uriStr);
            if (m.Success)
            {
                int major = int.Parse(m.Groups["major"].Value);
                int minor = int.Parse(m.Groups["minor"].Value);

                Version apiVersion = typeof(Code).Assembly.GetName().Version;
                if (apiVersion.Major < major
                        || apiVersion.Minor < minor)
                {
                    IsSkipped = true;
                }

                m = m.NextMatch();
                if (m.Success)
                {
                    major = int.Parse(m.Groups["major"].Value);
                    minor = int.Parse(m.Groups["minor"].Value);

                    if (apiVersion.Major > major
                            || apiVersion.Minor > minor)
                    {
                        IsSkipped = true;
                    }
                }
            }
        }

        public bool MatchesError(string errorMessage)
        {
            string errorsFile = Path.ChangeExtension(Uri.ToPath(), ".txt");

            if (File.Exists(errorsFile))
            {
                foreach (string line in File.ReadAllLines(errorsFile))
                {
                    if (new Regex(line).IsMatch(errorMessage))
                        return true;
                }
            }

            return false;
        }

        public override string ToString() => Name;
    }
}
