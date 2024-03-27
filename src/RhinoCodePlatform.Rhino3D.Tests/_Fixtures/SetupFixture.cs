using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [SetUpFixture]
    public sealed class SetupFixture : Rhino.Testing.Fixtures.RhinoSetupFixture
    {
        static readonly MxTestSettings s_settings;

        static SetupFixture()
        {
            string settingsFile = Rhino.Testing.Configs.Current.SettingsFile;

            if (File.Exists(settingsFile))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(MxTestSettings));
                    s_settings = Rhino.Testing.Configs.Deserialize<MxTestSettings>(serializer, settingsFile);

                    return;
                }
                catch (Exception) { }
            }

            s_settings = new MxTestSettings();
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

            LoadLanguages();
        }

        sealed class StatusResponder : ProgressStatusResponder
        {
            public override void StatusChanged(ILanguage language, ProgressChangedEventArgs args)
            {
                int progress = Convert.ToInt32(language.Status.Progress.Value * 100);
                Console.WriteLine($"Initializing languages {progress}%");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void LoadLanguages()
        {
            Registrar.StartScripting();
            Registrar.SendReportsToConsole = true;
            RhinoCode.Languages.WaitStatusComplete(LanguageSpec.Any, new StatusResponder());
            foreach (ILanguage language in RhinoCode.Languages)
            {
                if (language.Status.IsErrored)
                {
                    throw new Exception($"Language init error | {RhinoCode.Logger.Text}");
                }
            }
        }
    }
}
