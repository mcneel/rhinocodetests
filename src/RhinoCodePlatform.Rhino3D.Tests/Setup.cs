using System;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [SetUpFixture]
    public sealed class Setup : Rhino.Testing.Fixtures.RhinoSetupFixture
    {
        public override void OneTimeSetup()
        {
            base.OneTimeSetup();
            LoadLanguages();
        }

        sealed class StatusResponder : ProgressStatusResponder
        {
            public override void StatusChanged(ILanguage language, ProgressChangedEventArgs args)
            {
                int progress = Convert.ToInt32(language.Status.Progress.Value * 100);
                Console.WriteLine($"Initializing languages {progress}");
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        void LoadLanguages()
        {
            Rhino3DPlatform.Register();
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
