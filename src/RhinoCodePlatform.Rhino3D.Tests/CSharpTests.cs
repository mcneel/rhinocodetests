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
    public class CSharpTests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestCSharpScript(ScriptInfo scriptInfo)
        {
            Assume.That(scriptInfo.IsSkipped == false);

            Code code = GetLanguage(this, LanguageSpec.CSharp).CreateCode(scriptInfo.Uri);

            RunContext ctx;
            if (scriptInfo.IsDebug)
                ctx = new DebugContext();
            else
                ctx = new RunContext();

            ctx.OverrideCodeParams = true;
            ctx.Outputs["result"] = default;

            if (TryRunCode(scriptInfo, code, ctx, out string errorMessage))
            {
                Assert.True(ctx.Outputs.TryGet("result", out bool data));
                Assert.True(data);
            }
            else
                Assert.True(scriptInfo.MatchesError(errorMessage));
        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"cs\", "*.cs");
    }
}
