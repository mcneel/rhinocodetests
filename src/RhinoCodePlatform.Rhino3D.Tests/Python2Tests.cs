using System;
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
            TestSkip(scriptInfo);

            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(scriptInfo.Uri);

            RunContext ctx = GetRunContext();

            if (TryRunCode(scriptInfo, code, ctx, out string errorMessage))
            {
                Assert.True(ctx.Outputs.TryGet("result", out bool data));
                Assert.True(data);
            }
            else
                Assert.True(scriptInfo.MatchesError(errorMessage));
        }

        [Test]
        public void TestRuntimeErrorLine_InScript()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
@"import os

print(12 / 0)
");

            RunContext ctx = GetRunContext();

            try
            {
                code.Run(ctx);
            }
            catch (ExecuteException ex)
            {
                if (ex.Position.LineNumber != 3)
                    throw;
            }
        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py2\", "test_*.py");
    }
}
