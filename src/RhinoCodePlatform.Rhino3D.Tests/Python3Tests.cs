using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Tests;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Python3Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython3Script(ScriptInfo scriptInfo)
        {
            TestSkip(scriptInfo);

            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(scriptInfo.Uri);

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
        public void TestCompileErrorLine_ReturnOutsideFunction()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"a = ""Hello Python 3 in Grasshopper!""
print(a)

Some_Func()
return
");

            var ctx = new BuildContext();

            try
            {
                code.Build(ctx);
            }
            catch (CompileException ex)
            {
                if (ex.Diagnostics.First().Reference.Position.LineNumber != 5)
                    throw;
            }
        }

        [Test]
        public void TestRuntimeErrorLine_InScript()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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

        [Test]
        public void TestRuntimeErrorLine_InFunction()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"#! python 3
def foo():
    None[0]
foo()
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

        [Test]
        public void TestRuntimeErrorLine_InNestedFunctions()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"#! python 3

import sys
from System import Uri


def func_call_test(a, b):
    def nested_func_call_test(c):
        raise Exception(""I don't like you"")

    nested_func_call_test(a + b)


func_call_test(10, 10)


# some other code

func_call_test(5, 5)
");

            RunContext ctx = GetRunContext();

            try
            {
                code.Run(ctx);
            }
            catch (ExecuteException ex)
            {
                if (ex.Position.LineNumber != 9)
                    throw;
            }
        }

        [Test]
        public void TestDebugErrorLine_InNestedFunctions()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"#! python 3

import sys
from System import Uri


def func_call_test(a, b):
    def nested_func_call_test(c):
        raise Exception(""I don't like you"")

    nested_func_call_test(a + b)


func_call_test(10, 10)


# some other code

func_call_test(5, 5)
");

            // create debug controls to capture exception on line 9
            // note that a breakpoint on the line must be added. the controls
            // will stepOver the line to capture the exception event.
            var breakpoint = new CodeReferenceBreakpoint(code, 9);
            var controls = new ExceptionCaptureControls(breakpoint);

            var ctx = new DebugContext();

            code.DebugControls = controls;

            try
            {
                code.Debug(ctx);
            }
            catch (DebugStopException) { }
            catch (ExecuteException) { }
            catch (Exception) { throw; }
            finally
            {
                Assert.IsTrue(controls.Pass);
            }

        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py3\", "test_*.py");
    }
}
