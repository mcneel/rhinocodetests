using System;
using System.IO;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Serialization;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public abstract class ScriptFixture : Rhino.Testing.Fixtures.RhinoTestFixture
    {
        protected ILanguage m_language = default;

        protected static ILanguage GetLanguage(ScriptFixture fixture, LanguageSpec languageSpec)
        {
            if (fixture.m_language is null)
            {
                fixture.m_language = RhinoCode.Languages.QueryLatest(languageSpec);
                if (fixture.m_language is null)
                {
                    throw new Exception($"Language query error | {RhinoCode.Logger.Text}");
                }
            }

            return fixture.m_language;
        }

        protected static IEnumerable<object[]> GetTestScripts(string subPath, string fileFilter)
        {
            Rhino.Testing.Configs configs = Rhino.Testing.Configs.Current;

            if (SetupFixture.TryGetTestFiles(out string fileDir))
            {
                string fullpath = Path.GetFullPath(Path.Combine(configs.SettingsDir, fileDir, subPath));
                if (Directory.Exists(fullpath))
                {
                    foreach (var filePath in Directory.GetFiles(fullpath, fileFilter))
                        yield return new object[] { new ScriptInfo(new Uri(filePath)) };
                }
                else
                    yield break;
            }
        }

        protected static bool TryRunCode(ScriptInfo scriptInfo, Code code, RunContext context, out string errorMessage)
        {
            errorMessage = default;

            try
            {
                code.Run(context);
                return true;
            }
            catch (CompileException compileEx)
            {
                if (scriptInfo.ExpectsError)
                    errorMessage = compileEx.ToString();
                else
                    throw new Exception(compileEx.ToString());
            }
            catch (Exception runEx)
            {
                if (scriptInfo.ExpectsError)
                    errorMessage = runEx.ToString();
                else
                    throw;
            }

            return false;
        }
    }
}
