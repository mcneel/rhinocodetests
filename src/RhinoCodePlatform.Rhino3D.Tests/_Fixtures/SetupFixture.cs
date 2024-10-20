using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Logging;
using Rhino.Runtime.Code.Environments;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [SetUpFixture]
    public sealed class SetupFixture : Rhino.Testing.Fixtures.RhinoSetupFixture
    {
        public const string RHINOCODE_LOG_LEVEL_ENVVAR = "RHINOCODE_LOG_LEVEL";
        public const string RHINOCODE_PYTHON_VENV_PREFIX = "test_";

        static readonly TestSettings s_settings;

        static SetupFixture()
        {
            string settingsFile = Rhino.Testing.Configs.Current.SettingsFile;

            if (File.Exists(settingsFile))
            {
                try
                {
                    XmlSerializer serializer = new(typeof(TestSettings));
                    s_settings = Rhino.Testing.Configs.Deserialize<TestSettings>(serializer, settingsFile);

                    return;
                }
                catch (Exception) { }
            }

            s_settings = new TestSettings();
        }

        public static bool TryGetTestFiles(out string filesDir)
        {
            filesDir = default;

            if (Directory.Exists(s_settings.TestFilesDirectory))
            {
                filesDir = s_settings.TestFilesDirectory;
                return true;
            }

            return false;
        }

        public override void OneTimeSetup()
        {
            base.OneTimeSetup();

            if (!Directory.Exists(Rhino.Testing.Configs.Current.RhinoSystemDir))
            {
                throw new DirectoryNotFoundException(Rhino.Testing.Configs.Current.RhinoSystemDir);
            }

            LoadRhinoCode();
            LoadLanguages();
            LoadRhinoPlugins();
            LoadGrasshopperPlugins();
        }

        sealed class TestContextStatusResponder : ProgressStatusResponder
        {
#if RC8_13
            public override void LoadProgressChanged(LanguageLoadProgressReport value)
            {
                if (value.IsComplete)
                    TestContext.Progress.WriteLine($"Loading Languages Complete");
                else
                    TestContext.Progress.WriteLine($"Loading {value.Spec} ...");
            }
#endif
            public override void StatusChanged(ILanguage language, ProgressChangedEventArgs args)
            {
                // e.g.
                // Initializing Python 3.9.10: 6% - Deploying runtime
                int progress = Convert.ToInt32(language.Status.Progress.Value * 100);
                if (progress < 100)
                    TestContext.Progress.WriteLine($"Initializing {language.Id.Name} {language.Id.Version}: {progress,3}% - {language.Status.Progress.Message}");
                else
                    TestContext.Progress.WriteLine($"Initializing {language.Id.Name} {language.Id.Version}: Complete");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void LoadRhinoCode()
        {
            if (Environment.GetEnvironmentVariable(RHINOCODE_LOG_LEVEL_ENVVAR) is string level)
            {
                LogLevel logLevel = (LogLevel)0xFF;

                switch (level)
                {
                    case "trace":
                        logLevel = LogLevel.Trace;
                        break;

                    case "debug":
                        logLevel = LogLevel.Debug;
                        break;

                    case "info":
                        logLevel = LogLevel.Info;
                        break;

                    case "warn":
                    case "warning":
                        logLevel = LogLevel.Warn;
                        break;

                    case "error":
                        logLevel = LogLevel.Error;
                        break;

                    case "exception":
                    case "critical":
                        logLevel = LogLevel.Critical;
                        break;
                }

                TestContext.Progress.WriteLine($"RhinoCode Log Level: {logLevel}");
                RhinoCode.Logger.MessageLogged += (_, a) =>
                {
                    if (a.Level >= logLevel)
                        TestContext.Progress.WriteLine(a.Message);
                };
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void LoadLanguages()
        {
            Registrar.StartScripting();
#if RC8_11
            RhinoCode.ReportProgressToConsole = true;
#else
            Registrar.SendReportsToConsole = true;
#endif

#if RC8_14
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84389
            string netfwRefNuget = Path.Combine(RhinoCode.UserProfile, ".nuget", "packages", "microsoft.netframework.referenceassemblies.net48");
            if (Directory.Exists(netfwRefNuget))
            {
                Directory.Delete(netfwRefNuget, recursive: true);
            }
#endif
            RhinoCode.Languages.WaitStatusComplete(LanguageSpec.Any, new TestContextStatusResponder());
            foreach (ILanguage language in RhinoCode.Languages)
            {
                if (language.Status.IsErrored)
                {
                    throw new Exception($"Language init error | {RhinoCode.Logger.Text}");
                }

                // cleanup all python 3 environments before running tests
                if (LanguageSpec.Python3.Matches(language.Id))
                {
                    IEnvirons environs = language.Environs;
                    foreach (IEnviron environ in environs.QueryEnvirons()
                                                         .Where(e => e.Id.StartsWith(RHINOCODE_PYTHON_VENV_PREFIX)))
                    {
                        environs.DeleteEnviron(environ);
                    }
                }
            }

            string libsCache = RhinoCode.Stage.LanguageLibraries.Directory;
            if (Directory.Exists(libsCache))
            {
                Directory.Delete(libsCache, true);
                Directory.CreateDirectory(libsCache);
            }
        }

        static void LoadRhinoPlugins()
        {
            string testPluginsPath = Path.Combine(s_settings.TestFilesDirectory, "rhinoPlugins");

            if (Directory.Exists(testPluginsPath))
            {
                string libsdir = Path.Combine(testPluginsPath, "libs");
                if (Directory.Exists(libsdir))
                {
                    Directory.Delete(libsdir, true);
                }

                foreach (string rhpFile in Directory.GetFiles(testPluginsPath, "*.rhp", SearchOption.AllDirectories))
                {
                    TestContext.Progress.WriteLine($"Loading Rhino plugin: {rhpFile}");
                    Rhino.PlugIns.PlugIn.LoadPlugIn(rhpFile, out Guid _);
                }
            }
        }

        static void LoadGrasshopperPlugins()
        {
            string testPluginsPath = Path.Combine(s_settings.TestFilesDirectory, "gh1Plugins");

            if (Directory.Exists(testPluginsPath))
            {
                string libsdir = Path.Combine(testPluginsPath, "libs");
                if (Directory.Exists(libsdir))
                {
                    Directory.Delete(libsdir, true);
                }

                string[] ghaFiles = Directory.GetFiles(testPluginsPath, "*.gha", SearchOption.AllDirectories).ToArray();
                foreach (var ghaFile in ghaFiles)
                    TestContext.Progress.WriteLine($"Loading GH1 plugin: {ghaFile}");
                LoadGHA(ghaFiles);
            }
        }
    }
}
