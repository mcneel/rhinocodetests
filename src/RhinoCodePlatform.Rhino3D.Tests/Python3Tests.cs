using System;
using System.IO;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Python3Tests : Rhino.Testing.RhinoTestFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython3Script(Uri scriptPath)
        {
            var code = GetPython3().CreateCode(scriptPath);

            var ctx = new ExecuteContext
            {
                OverrideCodeParams = true,
                Outputs = {
                    ["result"] = default,
                },
            };

            code.Run(ctx);

            Assert.True(ctx.Outputs.TryGet("result", out bool data));
            Assert.True(data);
        }

        static object s_py3 = default;

        static ILanguage GetPython3()
        {
            if (s_py3 is null)
            {
                Rhino3DPlatform.Activate();
                RhinoCode.Languages.RespondToStatusWaits();

                ILanguage python3 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);
                
                python3.Status.WaitReady();
                
                Assert.NotNull(python3);

                s_py3 = python3;
            }

            return (ILanguage)s_py3;
        }

        static IEnumerable<object[]> GetTestScripts()
        {
            if (Configs.TryGetConfig("TestFilesDirectory", out string fileDir))
            {
                string fullpath = Path.GetFullPath(Path.Combine(Configs.SettingsDir, @"..\..\..\", fileDir, @"py3\"));
                if (Directory.Exists(fullpath))
                {
                    foreach (var filePath in Directory.GetFiles(fullpath, "*.py"))
                        yield return new object[] { new Uri(filePath) };
                }
                else
                    yield break;
            }
        }
    }
}
