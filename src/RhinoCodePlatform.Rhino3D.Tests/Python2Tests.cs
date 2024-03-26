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
    public class Python2Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython2Script(ScriptInfo scriptInfo)
        {
            Assume.That(scriptInfo.IsSkipped == false);

            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(scriptInfo.Uri);

            var ctx = new RunContext
            {
                OverrideCodeParams = true,
                Outputs = {
                    ["result"] = false,
                },
            };

            if (TryRunCode(scriptInfo, code, ctx, out string _))
            {
                Assert.True(ctx.Outputs.TryGet("result", out bool data));
                Assert.True(data);
            }
        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py2\", "*.py");
    }
}
