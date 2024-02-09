using System;
using System.IO;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

using NUnit.Framework;

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
                Assert.NotNull(fixture.m_language);
            }

            return fixture.m_language;
        }

        protected static IEnumerable<object[]> GetTestScripts(string subPath, string fileFilter)
        {
            Rhino.Testing.Configs configs = Rhino.Testing.Configs.Current;

            if (configs.TryGetConfig("TestFilesDirectory", out string fileDir))
            {
                string fullpath = Path.GetFullPath(Path.Combine(configs.SettingsDir, @"..\..\..\", fileDir, subPath));
                if (Directory.Exists(fullpath))
                {
                    foreach (var filePath in Directory.GetFiles(fullpath, fileFilter))
                        yield return new object[] { new ScriptInfo(new Uri(filePath)) };
                }
                else
                    yield break;
            }
        }

        protected bool TryRunCode(ScriptInfo scriptInfo, Code code, RunContext context)
        {
            try
            {
                code.Run(context);
                return true;
            }
            catch (CompileException compileEx)
            {
                if (!scriptInfo.ExpectsError)
                    throw new Exception(compileEx.ToString());
            }
            catch (Exception ex)
            {
                if (!scriptInfo.ExpectsError)
                    throw ex;
            }

            return false;
        }
    }
}
