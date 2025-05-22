#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
#pragma warning disable IDE0090 // Use 'new(...)'
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Storage;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public sealed class ScriptInfo
    {
        static readonly Regex s_rhinoVersionFinder = new Regex(@"(rc|rh|gh)(?<major>\d)\.(?<minor>\d{1,2})");
        static readonly Regex s_pythonVersionFinder = new Regex(@"(py|python)(?<major>\d)\.(?<minor>\d{1,2})");

        sealed class IsSkipped
        {
            public static IsSkipped No { get; } = new IsSkipped();

            readonly bool _skipped = false;
            readonly string _skippedMessage = string.Empty;
            IsSkipped() { }
            public IsSkipped(string reason)
            {
                _skipped = true;
                _skippedMessage = reason;
            }
            public void TestSkip()
            {
                if (_skipped)
                    Assert.Ignore(_skippedMessage);
            }
        }

        readonly IsSkipped _isSkipped = IsSkipped.No;

        public Uri Uri { get; }

        public string Name { get; }

        public bool IsAsync { get; } = false;

        public bool IsDebug { get; } = false;

        public bool IsProfile { get; } = false;

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
            ArgumentNullException.ThrowIfNull(scriptPath);

            string uriStr = scriptPath.ToString().ToLower();

            Uri = scriptPath;
            Name = Uri.GetEndpointTitle();
            IsAsync = uriStr.Contains("_async");
            IsDebug = uriStr.Contains("_debug");
            IsProfile = uriStr.Contains("_profile");
            ExpectsError = uriStr.Contains("_error");
            ExpectsWarning = uriStr.Contains("_warning");
            _isSkipped = uriStr.Contains("_skip") ? new IsSkipped("Specifically skipped by '_skip' in file name") : _isSkipped;

            Version apiVersion = typeof(Code).Assembly.GetName().Version;

            Match rh = s_rhinoVersionFinder.Match(uriStr);
            if (rh.Success)
            {
                int major = int.Parse(rh.Groups["major"].Value);
                int minor = int.Parse(rh.Groups["minor"].Value);
                Version minVersion = new Version(major, minor);

                if (apiVersion < minVersion)
                {
                    _isSkipped = new IsSkipped($"Rhino {minVersion} is too young for this test");
                }

                rh = rh.NextMatch();
                if (rh.Success)
                {
                    major = int.Parse(rh.Groups["major"].Value);
                    minor = int.Parse(rh.Groups["minor"].Value);
                    Version maxVersion = new Version(major, minor);

                    if (apiVersion > maxVersion)
                    {
                        _isSkipped = new IsSkipped($"Rhino {minVersion} is too matured for this test");
                    }
                }
            }

            Match py = s_pythonVersionFinder.Match(uriStr);
            if (py.Success)
            {
                int major = int.Parse(py.Groups["major"].Value);
                int minor = int.Parse(py.Groups["minor"].Value);
                Version pyVersion = new Version(major, minor);

                bool hasRequiredPython = RhinoCode.Languages.WherePasses(new LanguageSpec("*.python", pyVersion)).Any();
                _isSkipped = !hasRequiredPython ? new IsSkipped($"Required python {pyVersion} is not available") : _isSkipped;
            }

#if RELEASE
            _isSkipped = uriStr.Contains("_onlylocal") ? new IsSkipped($"Test is meant to be only run locally") : _isSkipped;
#endif

            ExpectsRhinoDocument = File.Exists(GetRhinoFile());
        }

        public void TestSkip() => _isSkipped.TestSkip();

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
