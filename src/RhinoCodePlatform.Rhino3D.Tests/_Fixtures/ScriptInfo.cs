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
        static readonly Regex s_performanceSpecFinder = new Regex(@"perf\((?<rounds>\d),\s*(?<mean>\d+)ms,\s*(?<dev>\d+)ms\)");

        public Uri Uri { get; }

        public string Name { get; }

        public bool IsDebug { get; } = false;

        public bool IsSkipped { get; } = false;

        public bool ExpectsError { get; } = false;

        public bool ExpectsWarning { get; } = false;

        public bool ExpectsRhinoDocument { get; } = false;

        #region Profiling
        public bool IsProfileTest { get; } = false;

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
            IsDebug = uriStr.Contains("_debug");
            IsSkipped = uriStr.Contains("_skip");
            ExpectsError = uriStr.Contains("_error");
            ExpectsWarning = uriStr.Contains("_warning");

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

            m = s_performanceSpecFinder.Match(uriStr);
            if (m.Success)
            {
                IsProfileTest = true;
                ProfileRounds = int.Parse(m.Groups["rounds"].Value);
                ExpectedMean = TimeSpan.FromMilliseconds(int.Parse(m.Groups["mean"].Value));
                ExpectedDeviation = TimeSpan.FromMilliseconds(int.Parse(m.Groups["dev"].Value));
            }

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
