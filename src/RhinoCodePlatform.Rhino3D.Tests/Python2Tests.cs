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
    public class Python2Tests : Rhino.Testing.RhinoTestFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython2Script(Uri scriptPath)
        {
            var code = GetPython2().CreateCode(scriptPath);

            var ctx = new ExecuteContext
            {
                OverrideCodeParams = true,
                Outputs = {
                    ["result"] = false,
                },
            };

            code.Run(ctx);

            Assert.True(ctx.Outputs.TryGet("result", out bool data));
            Assert.True(data);
        }

        static object s_py2 = default;

        static ILanguage GetPython2()
        {
            if (s_py2 is null)
            {
                Rhino3DPlatform.Activate();
                RhinoCode.Languages.RespondToStatusWaits();

                ILanguage python2 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python2);

                python2.Status.WaitReady();

                Assert.NotNull(python2);

                s_py2 = python2;
            }

            return (ILanguage)s_py2;
        }

        static IEnumerable<object[]> GetTestScripts()
        {
            if (Configs.TryGetConfig("TestFilesDirectory", out string fileDir))
            {
                string fullpath = Path.GetFullPath(Path.Combine(Configs.SettingsDir, @"..\..\..\", fileDir, @"py2\"));
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
