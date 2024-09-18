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
        static readonly Regex s_rhinoVersionFinder = new Regex(@"(rc|rh|gh)(?<major>\d)\.(?<minor>\d{1,2})");
        static readonly Regex s_rhinoLocalOnlyFinder = new Regex(@"_onlylocal");

        public Uri Uri { get; }

        public string Name { get; }

        public bool IsAsync { get; } = false;

        public bool IsDebug { get; } = false;

        public bool IsSkipped { get; } = false;

        public bool ExpectsError { get; } = false;

        public bool ExpectsWarning { get; } = false;

        public bool ExpectsRhinoDocument { get; } = false;

        #region Profiling
        public int ProfileRounds { get; } = 1;

        public TimeSpan ExpectedMean { get; } = TimeSpan.Zero;

        public TimeSpan ExpectedDeviation { get; } = TimeSpan.Zero;

        public TimeSpan ExpectedSlowest => ExpectedMean + ExpectedDeviation;

        public TimeSpan ExpectedFastest => ExpectedMean - ExpectedDeviation;
        #endregion

        public ScriptInfo(Uri scriptPath)
        {
            if (scriptPath is null)
                throw new ArgumentNullException(nameof(scriptPath));

            string uriStr = scriptPath.ToString().ToLower();

            Uri = scriptPath;
            Name = Uri.GetEndpointTitle();
            IsAsync = uriStr.Contains("_async");
            IsDebug = uriStr.Contains("_debug");
            IsSkipped = uriStr.Contains("_skip");
            ExpectsError = uriStr.Contains("_error");
            ExpectsWarning = uriStr.Contains("_warning");

            Version apiVersion = typeof(Code).Assembly.GetName().Version;

            Match m = s_rhinoVersionFinder.Match(uriStr);
            if (m.Success)
            {
                int major = int.Parse(m.Groups["major"].Value);
                int minor = int.Parse(m.Groups["minor"].Value);

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

#if RELEASE
            IsSkipped |= s_rhinoLocalOnlyFinder.IsMatch(uriStr);
#endif

            ExpectsRhinoDocument = File.Exists(GetRhinoFile());
        }

        public string GetRhinoFile() => Path.ChangeExtension(Uri.ToPath(), ".3dm");

        public string GetErrorsFile() => Path.ChangeExtension(Uri.ToPath(), ".txt");

        public bool MatchesError(string errorMessage)
        {
            string errorsFile = GetErrorsFile();

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
