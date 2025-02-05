using System;
using System.IO;

using Rhino.Runtime.InProcess;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Testing.Client
{
    public static class TestCases
    {
        public static int Run_InitPython3_FromScratch()
        {
            ResetPythonRuntime(RhinoCode.Directory, 3);

            using (new RhinoCore())
            {
                bool installed_get_pip = false;
                RhinoCode.Logger.Level = Rhino.Runtime.Code.Logging.LogLevel.Trace;
                RhinoCode.Logger.MessageLogged += (logger, arg) =>
                {
                    installed_get_pip |= arg.Message.Contains("pip (get-pip)");
                };

                LoadLanguages(LanguageSpec.Python3);

                ILanguage py = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);

                if (!py.Status.IsReady)
                {
                    return -1;
                }

                return installed_get_pip ? 0 : -2;
            }
        }

        public static int Run_InitPython3_FromScratch_NoInternet()
        {
            Environment.SetEnvironmentVariable("RHINOCODE_BLOCK_INTERNET", "true");
            ResetPythonRuntime(RhinoCode.Directory, 3);

            using (new RhinoCore())
            {
                bool installed_embedded = false;
                RhinoCode.Logger.Level = Rhino.Runtime.Code.Logging.LogLevel.Trace;
                RhinoCode.Logger.MessageLogged += (logger, arg) =>
                {
                    installed_embedded |= arg.Message.Contains("pip (embedded)");
                };

                LoadLanguages(LanguageSpec.Python3);

                ILanguage py = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);

                if (!py.Status.IsReady)
                {
                    return -1;
                }

                return installed_embedded ? 0 : -2;
            }
        }

        public static int Run_InitPython2_FromScratch()
        {
            ResetPythonRuntime(RhinoCode.Directory, 2);

            using (new RhinoCore())
            {
                LoadLanguages(LanguageSpec.Python2);

                ILanguage py = RhinoCode.Languages.QueryLatest(LanguageSpec.Python2);

                if (!py.Status.IsReady)
                {
                    return -1;
                }

                return 0;
            }
        }

        public static int Run_InitPython2_FromScratch_NoInternet()
        {
            Environment.SetEnvironmentVariable("RHINOCODE_BLOCK_INTERNET", "true");
            ResetPythonRuntime(RhinoCode.Directory, 2);

            using (new RhinoCore())
            {
                LoadLanguages(LanguageSpec.Python2);

                ILanguage py = RhinoCode.Languages.QueryLatest(LanguageSpec.Python2);

                if (!py.Status.IsReady)
                {
                    return -1;
                }

                return 0;
            }
        }

        sealed class ConsoleStatusResponder : ProgressStatusResponder
        {
            public override void LoadProgressChanged(LanguageLoadProgressReport value)
            {
                if (value.IsComplete)
                    Console.WriteLine($"Loaded Languages");
                else
#if RC9_0
                    Console.WriteLine($"Loading {value.LanguageSpec} ...");
#else
                    Console.WriteLine($"Loading {value.Spec} ...");
#endif
            }

            public override void StatusChanged(ILanguage language, ProgressChangedEventArgs args)
            {
                // e.g.
                // Initializing Python 3.9.10: 6% - Deploying runtime
                int progress = Convert.ToInt32(language.Status.Progress.Value * 100);
                if (progress < 100)
                    Console.WriteLine($"Initializing {language.Id.Name} {language.Id.Version}: {progress,3}% - {language.Status.Progress.Message}");
                else
                    Console.WriteLine($"Initializing {language.Id.Name} {language.Id.Version}: Complete");
            }
        }

        static void ResetPythonRuntime(string rhinocode, int major)
        {
            foreach (var dir in Directory.GetDirectories(rhinocode))
            {
                string dirname = Path.GetFileName(dir);
                if (dirname.StartsWith($"py{major}")
                        && Path.Combine(dir, "config.json") is string config
                        && File.Exists(config))
                {
                    Console.WriteLine($"Resetting Python {major} runtime config @ {config}");
                    File.Delete(config);
                }
            }
        }

        static void LoadLanguages(LanguageSpec spec)
        {
            Registrar.StartScripting();
            RhinoCode.ReportProgressToConsole = true;
            RhinoCode.Languages.WaitStatusComplete(spec, new ConsoleStatusResponder());
        }
    }
}
