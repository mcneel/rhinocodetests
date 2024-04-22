using System;
using System.Linq;
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
            TestSkip(scriptInfo);

            Code code = GetLanguage(this, LanguageSpec.CSharp).CreateCode(scriptInfo.Uri);

            RunContext ctx;
            if (scriptInfo.IsDebug)
                ctx = new DebugContext();
            else
                ctx = new RunContext();

            ctx.OutputStream = GetOutputStream();
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

        [Test]
        public void TestCompileErrorLine_MissingFunction()
        {
            Code code = GetLanguage(this, LanguageSpec.CSharp).CreateCode(
@"
using System;

DoOtherStuff();

void DoStuff(int s)
{
    // try
    // {
    var k;



    var z = Joe.One;
}
");

            var ctx = new BuildContext();

            try
            {
                code.Build(ctx);
            }
            catch (CompileException ex)
            {
                if (ex.Diagnostics.First().Reference.Position.LineNumber != 4)
                    throw;
            }
        }

        [Test]
        public void TestRuntimeErrorLine_InScript()
        {
            Code code = GetLanguage(this, LanguageSpec.CSharp).CreateCode(
@"
using System;

int a = 1 + 2;
int zero = 0;
a = 5 / zero;
");

            RunContext ctx = GetRunContext();

            try
            {
                code.Run(ctx);
            }
            catch (ExecuteException ex)
            {
                if (ex.Position.LineNumber != 6)
                    throw;
            }
        }

        [Test]
        public void TestRuntimeErrorLine_InFunction()
        {
            Code code = GetLanguage(this, LanguageSpec.CSharp).CreateCode(
@"
using System;

DoOtherStuff();

void DoStuff(int s)
{
    // try
    // {
    var k = 12;



    var z = Joe.One;
    var y = new Jose();
    var m = new Uri(""file:///"");
    var n = new Jack();
    
    
    var f = 12 / s;

    // Console.WriteLine(12 / s);
    // }
    // catch (Exception ex)
    // {
    //     Console.WriteLine(ex);
    // }
}

void DoOtherStuff() { DoStuff(0); }

enum Joe {
    One,
    Two,
}

class Jack { }

struct Jose { }

");

            RunContext ctx = GetRunContext();

            try
            {
                code.Run(ctx);
            }
            catch (ExecuteException ex)
            {
                if (ex.Position.LineNumber != 20)
                    throw;
            }
        }

        // FIXME: Move csharp autocompletion to language module
        //        [Test]
        //        public void TestComplete_System_Console()
        //        {
        //            Code code = GetLanguage(this, LanguageSpec.CSharp).CreateCode(
        //@"
        //using System;
        //using Rhino;

        //Console.");

        //            string text = code.Text;
        //            IEnumerable<CompletionInfo> completions =
        //                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

        //            CompletionInfo cinfo;
        //            bool result = true;

        //            cinfo = completions.First(c => c.Text == "WriteLine");
        //            result &= CompletionKind.Function == cinfo.Kind;

        //            cinfo = completions.First(c => c.Text == "WindowWidth");
        //            result &= CompletionKind.Property == cinfo.Kind;

        //            Assert.True(result);
        //        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"cs\", "test_*.cs");
    }
}
