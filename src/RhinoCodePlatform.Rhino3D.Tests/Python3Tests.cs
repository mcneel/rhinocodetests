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
    public class Python3Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython3Script(Uri scriptPath)
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(scriptPath);

            var ctx = new RunContext
            {
                OverrideCodeParams = true,
                Outputs = {
                    ["result"] = default,
                },
            };

            RunCompiledCode(code, ctx);

            Assert.True(ctx.Outputs.TryGet("result", out bool data));
            Assert.True(data);
        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py3\", "*.py");
    }
}
