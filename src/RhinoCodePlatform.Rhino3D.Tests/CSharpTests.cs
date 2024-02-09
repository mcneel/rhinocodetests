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
        public void TestCSharpScript(Uri scriptPath)
        {
            Code code = GetLanguage(this, LanguageSpec.CSharp).CreateCode(scriptPath);

            RunContext ctx;
            if (scriptPath.ToString().Contains("_DEBUG"))
                ctx = new DebugContext();
            else
                ctx = new RunContext();

            ctx.OverrideCodeParams = true;
            ctx.Outputs["result"] = default;

            RunCompiledCode(code, ctx);

            Assert.True(ctx.Outputs.TryGet("result", out bool data));
            Assert.True(data);
        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"cs\", "*.cs");
    }
}
