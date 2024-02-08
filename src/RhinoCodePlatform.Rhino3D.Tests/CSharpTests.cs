using System;
using System.IO;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class CSharpTests : Rhino.Testing.RhinoTestFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestCSharpScript(Uri scriptPath)
        {
            Code code = GetCSharp().CreateCode(scriptPath);

            RunContext ctx;
            if (scriptPath.ToString().Contains("_DEBUG"))
                ctx = new DebugContext();
            else
                ctx = new RunContext();

            ctx.OverrideCodeParams = true;
            ctx.Outputs["result"] = default;

            Utils.RunCode(code, ctx);

            Assert.True(ctx.Outputs.TryGet("result", out bool data));
            Assert.True(data);
        }

        static object s_cs = default;

        static ILanguage GetCSharp()
        {
            if (s_cs is null)
            {
                ILanguage csharp = RhinoCode.Languages.QueryLatest(LanguageSpec.CSharp);

                Assert.NotNull(csharp);

                s_cs = csharp;
            }

            return (ILanguage)s_cs;
        }

        static IEnumerable<object[]> GetTestScripts()
        {
            if (Configs.TryGetConfig("TestFilesDirectory", out string fileDir))
            {
                string fullpath = Path.GetFullPath(Path.Combine(Configs.SettingsDir, @"..\..\..\", fileDir, @"cs\"));
                if (Directory.Exists(fullpath))
                {
                    foreach (var filePath in Directory.GetFiles(fullpath, "*.cs"))
                        yield return new object[] { new Uri(filePath) };
                }
                else
                    yield break;
            }
        }
    }
}
