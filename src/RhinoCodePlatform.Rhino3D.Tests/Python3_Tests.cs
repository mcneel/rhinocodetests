using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Text;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Testing;

#if RC8_11
using RhinoCodePlatform.Rhino3D.Languages.GH1;
#else
using RhinoCodePlatform.Rhino3D.Languages;
#endif

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Python3_Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython3_Script(ScriptInfo scriptInfo)
        {
            TestSkip(scriptInfo);

            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(scriptInfo.Uri);

            RunContext ctx = GetRunContext(scriptInfo);

            if (TryRunCode(scriptInfo, code, ctx, out string errorMessage))
            {
                Assert.True(ctx.Outputs.TryGet("result", out bool data));
                Assert.True(data);
            }
            else
                Assert.True(scriptInfo.MatchesError(errorMessage));
        }

        [Test]
        public void TestPython3_CompileErrorLine_ReturnOutsideFunction()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
a = ""Hello Python 3 in Grasshopper!""
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
#if RC8_11
                if (ex.Diagnosis.First().Reference.Position.LineNumber != 6)
#else
                if (ex.Diagnostics.First().Reference.Position.LineNumber != 6)
#endif
                    throw;
            }
        }

        [Test]
        public void TestPython3_Compile_Script()
        {
            // assert throws compile exception on run/debug/profile
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import os

a = None[0
");


            code.DebugControls = new DebugContinueAllControls();
            code.Profiler = EmptyProfiler.Default;

            ExecuteException run = Assert.Throws<ExecuteException>(() => code.Run(new RunContext()));
            Assert.IsInstanceOf(typeof(CompileException), run.InnerException);

            ExecuteException debug = Assert.Throws<ExecuteException>(() => code.Debug(new DebugContext()));
            Assert.IsInstanceOf(typeof(CompileException), debug.InnerException);

            ExecuteException profile = Assert.Throws<ExecuteException>(() => code.Profile(new ProfileContext()));
            Assert.IsInstanceOf(typeof(CompileException), profile.InnerException);
        }

        [Test]
        public void TestPython3_RuntimeErrorLine_InScript()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import os

print(12 / 0)
");

            RunContext ctx = GetRunContext();

            try
            {
                code.Run(ctx);
            }
            catch (ExecuteException ex)
            {
                if (ex.Position.LineNumber != 4)
                    throw;
            }
        }

        [Test]
        public void TestPython3_RuntimeErrorLine_InFunction()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
def foo():
    None[0]
foo()
");

            // No need to capture stdout.
            // otherwise it reports SyntaxError in console during build
            // since None[0] is invalid syntax
            RunContext ctx = GetRunContext(captureStdout: false);

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
        public void TestPython3_RuntimeErrorLine_InNestedFunctions()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"

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
        public void TestPython3_DebugStop()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import sys
print(sys) # line 3
");

            var breakpoint = new CodeReferenceBreakpoint(code, 3);
            var controls = new DebugStopperControls(breakpoint);

            var ctx = new DebugContext();

            code.DebugControls = controls;


            Assert.Throws<DebugStopException>(() => code.Debug(ctx));
        }

        [Test]
        public void TestPython3_DebugErrorLine_InNestedFunctions()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"

import sys
from System import Uri


def func_call_test(a, b):
    def nested_func_call_test(c):
        raise Exception(""I don't like you"")  # line 9

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

            bool capturedException = false;
            var ctx = new DebugContext();

            code.DebugControls = controls;

            try
            {
                code.Debug(ctx);
            }
            catch (ExecuteException)
            {
                capturedException = true;
            }
            catch (Exception) { throw; }
            finally
            {
                Assert.IsTrue(controls.Pass && capturedException);
            }
        }

        [Test]
        public void TestPython3_Complete_RhinoScriptSyntax()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import rhinoscriptsyntax as rs
rs.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "AddAlias");
            result &= CompletionKind.Function == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "rhapp");
            result &= CompletionKind.Module == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "rhcommand");
            result &= CompletionKind.Class == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "Rhino");
            result &= CompletionKind.Value == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_Complete_RhinoCommon_Rhino()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "RhinoApp");
            result &= CompletionKind.Class == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "Runtime");
            result &= CompletionKind.Module == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_Complete_RhinoCommon_Point3d()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
from Rhino.Geometry import Point3d
p = Point3d()
p.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "Add");
            // NOTE: this really should be method!
            result &= CompletionKind.Function == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "X");
            result &= CompletionKind.Property == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_Complete_RhinoCommon_ProxyTypes_NONE()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Render.ProxyTypes.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "NONE");
            result &= CompletionKind.EnumMember == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_Complete_StdLib_os()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import os
os.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "abc");
            result &= CompletionKind.Module == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "environ");
            result &= CompletionKind.Value == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_Complete_StdLib_os_path()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import os.path as op
op.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "dirname");
            result &= CompletionKind.Function == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "curdir");
            result &= CompletionKind.Text == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_Complete_str_array()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
a = [str()];
a[0].");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "capitalize");
            result &= CompletionKind.Function == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "split");
            result &= CompletionKind.Function == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_Complete_Enum_Members()
        {
            SkipBefore(8, 8);

            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
from enum import Enum

class TestEnum(Enum):
    ONE = 1
    TWO = 2

    @classmethod
    def test_class_method(cls):
        return cls(2)

m = TestEnum.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
#if RC8_9
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#else
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);
#endif

            Assert.True(completions.Any(c => c.Text == "test_class_method"));
        }

        [Test]
        public void TestPython3_PIP_SitePackage()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81895
            string pkgPath = string.Empty;

            ILanguage py3 = GetLanguage(LanguageSpec.Python3);
            Code code = py3.CreateCode(
$@"
# venv: site-packages
#r: rx
import rx

{nameof(pkgPath)} = rx.__file__
");

            RunContext ctx = GetRunContext();
            ctx.Outputs.Set(nameof(pkgPath), pkgPath);

            code.Run(ctx);

            pkgPath = ctx.Outputs.Get<string>(nameof(pkgPath));
            Assert.True(new Regex(@"[Ll]ib[\\/]site-packages[\\/]").IsMatch(pkgPath));
        }

        [Test]
        public void TestPython3_PIP_SitePackage_Shared()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81895
            ILanguage py3 = GetLanguage(LanguageSpec.Python3);
            Code code = py3.CreateCode(
@"
# venv: site-packages
#r: fpdf
import fpdf
");

            string pkgPath = string.Empty;
            RunContext ctx = GetRunContext();
            code.Run(ctx);

            code = py3.CreateCode(
$@"
#r: fpdf
import fpdf

{nameof(pkgPath)} = fpdf.__file__
");

            ctx = GetRunContext();
            ctx.Outputs.Set(nameof(pkgPath), pkgPath);

            code.Run(ctx);

            pkgPath = ctx.Outputs.Get<string>(nameof(pkgPath));
            Assert.True(new Regex(@"[Ll]ib[\\/]site-packages[\\/]").IsMatch(pkgPath));
        }

#if RC8_8
        [Test]
        public void TestPython3_Debug_Variables_Enum_CheckValue()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
from Rhino.DocObjects import ObjectType
m = ObjectType.AnyObject
stop = m # line 4
");

            var breakpoint = new CodeReferenceBreakpoint(code, 4);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("m", Rhino.DocObjects.ObjectType.AnyObject),
            })
            {
            };

            code.DebugControls = controls;
            var ctx = new DebugContext();
            code.Debug(ctx);

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_Debug_Variables_Enum_ShouldNotExpand()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
from Rhino.DocObjects import ObjectType
m = ObjectType.Brep
stop = m # line 4
");

            var breakpoint = new CodeReferenceBreakpoint(code, 4);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("m", Rhino.DocObjects.ObjectType.Brep),
            })
            {
                OnReceivedExpected = (v) =>
                {
                    // enum value of "m" must not be expandable in debugger
                    if (GetIdentifier(v) == "m")
                        return !v.CanExpand;
                    return true;
                }
            };

            code.DebugControls = controls;
            var ctx = new DebugContext();
            code.Debug(ctx);

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_Debug_Variables_RhinoObject()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
from Rhino.Geometry import Sphere, Point3d
doc: Rhino.RhinoDoc = Rhino.RhinoDoc.ActiveDoc
brep = Sphere(Point3d.Origin, 10).ToBrep()
brep_id = doc.Objects.AddBrep(brep)
brep_obj = doc.Objects.Find(brep_id)
stop = brep_obj # line 8
");

            var breakpoint = new CodeReferenceBreakpoint(code, 8);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("brep_obj"),
            })
            {
                OnReceivedExpected = (v) =>
                {
                    if (GetIdentifier(v) == "brep_obj")
                    {
                        ExecVariable[] members = v.Expand().ToArray();

                        Assert.IsTrue(members.Any(m => GetIdentifier(m) == "Geometry"));

                        // Guid is not exapandable
                        ExecVariable id = members.First(m => GetIdentifier(m) == "Id");
                        Assert.IsFalse(id.CanExpand);

                        // bool is not exapandable
                        ExecVariable isHidden = members.First(m => GetIdentifier(m) == "IsHidden");
                        Assert.IsFalse(isHidden.CanExpand);

                        // None is not exapandable
                        ExecVariable renderMaterial = members.First(m => GetIdentifier(m) == "RenderMaterial");
                        Assert.IsFalse(isHidden.CanExpand);

                        ExecVariable[] expanded;

                        // assert enumerable with one item has [0] and Count
                        ExecVariable geom = members.First(m => GetIdentifier(m) == "Geometry");
                        ExecVariable edges = geom.Expand().First(m => GetIdentifier(m) == "Edges");
                        expanded = edges.Expand().ToArray();
                        Assert.Greater(expanded.Length, 2);
                        Assert.Contains("[0]", expanded.Select(e => GetIdentifier(e)).ToList());
                        Assert.Contains("Count", expanded.Select(e => GetIdentifier(e)).ToList());

                        // assert array only has one Length member
                        ExecVariable subobjMat = members.First(m => GetIdentifier(m) == "SubobjectMaterialComponents");
                        expanded = subobjMat.Expand().ToArray();
                        Assert.AreEqual(1, expanded.Length);
                        Assert.AreEqual("Length", GetIdentifier(expanded[0]));

                        // assert color is expandable
                        ExecVariable attribs = members.First(m => GetIdentifier(m) == "Attributes");
                        ExecVariable objColor = attribs.Expand().First(m => GetIdentifier(m) == "ObjectColor");
                        Assert.IsTrue(objColor.CanExpand);
                    }

                    return true;
                }
            };

            code.DebugControls = controls;
            var ctx = new DebugContext();
            code.Debug(ctx);

            Assert.True(controls.Pass);
        }
#endif

#if RC8_9
        [Test]
        public void TestPython3_StdErr()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import sys

result = sys.stderr is not None
");

#if RC8_12
            var ctx = new RunContext(defaultErrorStream: true)
#else
            var ctx = new RunContext(defaultStderr: true)
#endif
            {
                AutoApplyParams = true,
                Outputs = { ["result"] = false }
            };

            code.Run(ctx);

            Assert.IsTrue(ctx.Outputs.Get<bool>("result"));
        }

        [Test]
        public void TestPython3_DebugPauses_Script_StepOver()
        {
            // python 3 debugger does not stop on 'pass' statements
            // so using Test() instead
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
def Test():
    pass

def First():
    Test() # line 6
    Test() # line 7

First()
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepOver);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 7));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_DebugPauses_Script_StepOut()
        {
            // python 3 debugger does not stop on 'pass' statements
            // so using Test() instead
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
def Test():
    pass

def First():
    Test() # line 6
    Test() # line 7

First()
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepOut);
            controls.DoNotExpectPause(new CodeReferenceBreakpoint(code, 7));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_Diagnose_SuperInit_PythonClass()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
class Base:
    def __init__(self):
        pass

class Derived(Base):
    def __init__(self):
        super().__init__()

class MissingSuper(Base):
    def __init__(self):
        pass
");

            IEnumerable<Diagnostic> diagnostics =
                code.Language.Support.Diagnose(SupportRequest.Empty, code, s_errorsOnly);

            Assert.IsEmpty(diagnostics);
        }

        [Test]
        public void TestPython3_Diagnose_SuperInit_RhinoCommon()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
from Rhino.Geometry import Point3d

class NewPoint(Point3d): # line 4, Point3d is a struct
    def __init__(self):
        pass
");

            IEnumerable<Diagnostic> diagnostics =
                code.Language.Support.Diagnose(SupportRequest.Empty, code, s_errorsOnly);

            Assert.AreEqual(1, diagnostics.Count());
            Diagnostic first = diagnostics.First();
            Assert.AreEqual("E:superchecker", first.Id);
            Assert.AreEqual(4, first.Reference.Position.LineNumber);
            Assert.AreEqual("\"NewPoint\" class is missing super().__init__() in its initializer for base class \"Point3d\"", first.Message);
        }

        [Test]
        public void TestPython3_Diagnose_SuperInit_RhinoCommon_ImportAs()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino.Input.Custom as ric

class InheritedGetPoint(ric.GetPoint): # line 4
    def __init__(self):
        pass
");

            IEnumerable<Diagnostic> diagnostics =
                code.Language.Support.Diagnose(SupportRequest.Empty, code, s_errorsOnly);

            Assert.AreEqual(1, diagnostics.Count());
            Diagnostic first = diagnostics.First();
            Assert.AreEqual("E:superchecker", first.Id);
            Assert.AreEqual(4, first.Reference.Position.LineNumber);
            Assert.AreEqual("\"InheritedGetPoint\" class is missing super().__init__() in its initializer for base class \"ric.GetPoint\"", first.Message);
        }

        [Test]
        public void TestPython3_Diagnose_SuperInit_EtoForms()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
from Eto.Forms import Form

class NewForm(Form):
    def __init__(self):
        pass
");

            IEnumerable<Diagnostic> diagnostics =
                code.Language.Support.Diagnose(SupportRequest.Empty, code, s_errorsOnly);

            Assert.AreEqual(1, diagnostics.Count());
            Diagnostic first = diagnostics.First();
            Assert.AreEqual("E:superchecker", first.Id);
            Assert.AreEqual(4, first.Reference.Position.LineNumber);
            Assert.AreEqual("\"NewForm\" class is missing super().__init__() in its initializer for base class \"Form\"", first.Message);
        }

        [Test]
        public void TestPython3_Complete_Import()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import ");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

            Assert.IsNotEmpty(completions);
        }

        // https://mcneel.myjetbrains.com/youtrack/issue/RH-81189
        [Test]
        public void TestPython3_Complete_LastIndex()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            string text = code.Text;
            completions = support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_Complete_AfterSingleQuoteComment()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86072
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
'""'
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 35, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_Complete_AfterDoubleQuoteComment()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86072
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
""'""
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 35, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InCommentBlock()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
""""""
This is a comment block
Rhino.
""""""
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 52, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 73, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InCommentBlock_Nested()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
""""""
Do not show ""complete Rhino.
Do not show ""complete"" Rhino.
Do not show \""complete Rhino.
Do not show \""complete\"" Rhino.

Do not show 'complete Rhino.
Do not show 'complete' Rhino.
Do not show \'complete Rhino.
Do not show \'complete\' Rhino.
""""""
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 57, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 88, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 119, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 152, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 184, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 215, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 246, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 279, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 284, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 292, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InCommentBlock_Start()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"""""""
");

            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, 3, CompleteOptions.Empty);

            Assert.IsEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InFunctionDocstring()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
def Foo() -> None: # Rhino.
    """"""Some Foo Function

    Args:
        Rhino.

    """"""
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 106, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 117, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 125, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InLiteralString_Double()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
m = ""Rhino.""
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 35, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 44, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InLiteralString_DoubleEscaped()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
m = ""\""Rhino.""
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 37, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 46, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InLiteralString_Single()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
m = 'Rhino.'
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 35, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 44, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InLiteralString_SingleEscaped()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.
m = '\'Rhino.'
Rhino.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, 22, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 37, CompleteOptions.Empty);
            Assert.IsEmpty(completions);

            completions = support.Complete(SupportRequest.Empty, code, 46, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteSignature()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject(");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
#if RC8_15
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteSignatureOptions.Empty);
#else
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#endif

            Assert.AreEqual(2, signatures.Count());

            SignatureInfo sig;

#if RC9_0
            // for some reason in Rhino 9, the list order is different
            signatures = signatures.Reverse();
#endif

            sig = signatures.ElementAt(0);
            Assert.AreEqual(0, sig.ParameterIndex);
            Assert.AreEqual(
                @"GetOneObject(prompt: str, acceptNothing: bool, filter_: DocObjects.ObjectType) -> (Commands.Result, DocObjects.ObjRef)",
                sig.Text
                );
            Assert.AreEqual("prompt: str", sig.Parameters[0].Name);
            Assert.AreEqual("acceptNothing: bool", sig.Parameters[1].Name);
            Assert.AreEqual("filter_: DocObjects.ObjectType", sig.Parameters[2].Name);

            sig = signatures.ElementAt(1);
            Assert.AreEqual(0, sig.ParameterIndex);
            Assert.AreEqual(
                @"GetOneObject(prompt: str, acceptNothing: bool, filter_: Custom.GetObjectGeometryFilter) -> (Commands.Result, DocObjects.ObjRef)",
                sig.Text
                );
            Assert.AreEqual("prompt: str", sig.Parameters[0].Name);
            Assert.AreEqual("acceptNothing: bool", sig.Parameters[1].Name);
            Assert.AreEqual("filter_: Custom.GetObjectGeometryFilter", sig.Parameters[2].Name);
        }

        [Test]
        public void TestPython3_CompleteSignature_ParameterIndex()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject(prompt, ");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
#if RC8_15
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteSignatureOptions.Empty);
#else
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#endif

            Assert.AreEqual(2, signatures.Count());

            SignatureInfo sig;

            sig = signatures.ElementAt(0);
            Assert.AreEqual(1, sig.ParameterIndex);

            sig = signatures.ElementAt(1);
            Assert.AreEqual(1, sig.ParameterIndex);
        }

        [Test]
        public void TestPython3_Format_Simple()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
class Test:

    def __init__(self):
        pass
");

            string result = code.Language.Support.Format(SupportRequest.Empty, code, FormatOptions.Empty);

            Assert.AreEqual(
@"class Test:
    def __init__(self):
        pass
", result);
        }

        [Test]
        public void TestPython3_PIP_AccessDeniedError()
        {
            const string P = "#";

            // https://github.com/mcneel/rhino/pull/72450
            ILanguage py3 = GetLanguage(LanguageSpec.Python3);

            py3.CreateCode(
$@"
{P} venv: {SetupFixture.RHINOCODE_PYTHON_VENV_PREFIX}access_denied
{P} r: openexr
import OpenEXR
").Run(GetRunContext());


            Code code = py3.CreateCode(
$@"
{P} venv: {SetupFixture.RHINOCODE_PYTHON_VENV_PREFIX}access_denied
{P} r: openexr-tools
import OpenEXR
");

            EnvironException restore = Assert.Throws<EnvironException>(() => code.Run(GetRunContext()));
            Assert.IsTrue(restore.Message.StartsWith("Access Denied: "));

            CorruptEnvironException badRun = Assert.Throws<CorruptEnvironException>(() => code.Run(GetRunContext()));
            Assert.IsTrue(badRun.Message.Contains("is corrupted"));
        }

        [Test]
        public void TestPython3_PIP_Install()
        {
            // https://github.com/mcneel/rhino/pull/72450
            ILanguage py3 = GetLanguage(LanguageSpec.Python3);

            Assert.NotNull(py3.Environs.Shared);

            IEnviron environ = py3.Environs.CreateEnviron($"{SetupFixture.RHINOCODE_PYTHON_VENV_PREFIX}install_requests");

            IPackage pkg = environ.AddPackage(new PackageSpec("requests", "2.31.0"));
            Assert.AreEqual("requests==2.31.0 (Any)", pkg.ToString());
            Assert.AreEqual("requests", pkg.Id);
            Assert.AreEqual("2.31.0", pkg.Version.ToString());
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""
a = ""Hello Python 3 in Grasshopper!""
print(a)

");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 3
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        a = ""Hello Python 3 in Grasshopper!""
        print(a)
        
        
        return
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_LastEmptyLine()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
print(a)");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 3
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        print(a)
        return
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_CommentBlock()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script

Hello Python 3 in Grasshopper!
Hello Python 3 in Grasshopper!

Hello Python 3 in Grasshopper!

""""""
a = ""Hello Python 3 in Grasshopper!""
print(a)

");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 3
""""""Grasshopper Script

Hello Python 3 in Grasshopper!
Hello Python 3 in Grasshopper!

Hello Python 3 in Grasshopper!

""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        a = ""Hello Python 3 in Grasshopper!""
        print(a)
        
        
        return
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_WithFunction()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""

a = 42
print(""test"")

def Test():
    pass


a = 12
");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 3
""""""Grasshopper Script""""""

import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        a = 42
        print(""test"")
        
        return


def Test():
    pass


a = 12
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_AddSolveOverrides()
        {
            const string P = "#";
            var script = new Grasshopper1Script(@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: false);

            Assert.AreEqual($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return

    {P} Solve overrides 
    def BeforeRunScript(self):
        pass

    def AfterRunScript(self):
        pass
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_AddPreviewOverrides()
        {
            const string P = "#";
            var script = new Grasshopper1Script(@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: false, addPreview: true);

#if RC8_18
            Assert.AreEqual($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return

    {P} Preview overrides 
    def get_ClippingBox(self):
        return Rhino.Geometry.BoundingBox.Empty

    def DrawViewportWires(self, args):
        pass

    def DrawViewportMeshes(self, args):
        pass
", script.Text);
#else
            Assert.AreEqual($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return

    {P} Preview overrides 
    @property
    def ClippingBox(self):
        return Rhino.Geometry.BoundingBox.Empty

    def DrawViewportWires(self, args):
        pass

    def DrawViewportMeshes(self, args):
        pass
", script.Text);
#endif
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_AddBothOverrides()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: true);

#if RC8_18
            Assert.AreEqual($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return

    {P} Solve overrides 
    def BeforeRunScript(self):
        pass

    def AfterRunScript(self):
        pass

    {P} Preview overrides 
    def get_ClippingBox(self):
        return Rhino.Geometry.BoundingBox.Empty

    def DrawViewportWires(self, args):
        pass

    def DrawViewportMeshes(self, args):
        pass
", script.Text);
#else
            Assert.AreEqual($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return

    {P} Solve overrides 
    def BeforeRunScript(self):
        pass

    def AfterRunScript(self):
        pass

    {P} Preview overrides 
    @property
    def ClippingBox(self):
        return Rhino.Geometry.BoundingBox.Empty

    def DrawViewportWires(self, args):
        pass

    def DrawViewportMeshes(self, args):
        pass
", script.Text);
#endif
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_AddBothOverrides_Steps()
        {
            const string P = "#";
            var script = new Grasshopper1Script(@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: false);
            script.ConvertToScriptInstance(addSolve: false, addPreview: true);

#if RC8_18
            Assert.AreEqual($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return

    {P} Solve overrides 
    def BeforeRunScript(self):
        pass

    def AfterRunScript(self):
        pass

    {P} Preview overrides 
    def get_ClippingBox(self):
        return Rhino.Geometry.BoundingBox.Empty

    def DrawViewportWires(self, args):
        pass

    def DrawViewportMeshes(self, args):
        pass
", script.Text);
#else
            Assert.AreEqual($@"#! python 3
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return

    {P} Solve overrides 
    def BeforeRunScript(self):
        pass

    def AfterRunScript(self):
        pass

    {P} Preview overrides 
    @property
    def ClippingBox(self):
        return Rhino.Geometry.BoundingBox.Empty

    def DrawViewportWires(self, args):
        pass

    def DrawViewportMeshes(self, args):
        pass
", script.Text);
#endif
        }

        [Test]
        public void TestPython3_ScriptInstance_Complete_Self()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y):
        self.
        return

");

            Code code = script.CreateCode();

            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, 229, CompleteOptions.Empty);

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "Iteration");
            result &= CompletionKind.Property == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "RhinoDocument");
            result &= CompletionKind.Property == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "GrasshopperDocument");
            result &= CompletionKind.Property == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "Component");
            result &= CompletionKind.Property == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "Print");
            result &= CompletionKind.Function == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "Reflect");
            result &= CompletionKind.Function == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "AddRuntimeMessage");
            result &= CompletionKind.Function == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython3_ScriptInstance_Complete_SelfRhinoDoc()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y):
        self.RhinoDocument.
        return

");

            Code code = script.CreateCode();

            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, 243, CompleteOptions.Empty);

            CompletionInfo cinfo;
            bool result = true;

            cinfo = completions.First(c => c.Text == "ActiveCommandId");
            result &= CompletionKind.Property == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "OpenDocuments");
            result &= CompletionKind.Function == cinfo.Kind;

            Assert.True(result);
        }
#endif

#if RC8_10
        [Test]
        public void TestPython3_Diagnose_SuperInit_RhinoCommon_GetObject()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82559
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
import scriptcontext as sc
import rhinoscriptsyntax as rs
import math

class GO_FilterPrevious(Rhino.Input.Custom.GetObject):
    def __init__(self, ids):
        self.m_ids = ids
        self.SubObjectSelect = False
");

            IEnumerable<Diagnostic> diagnostics =
                code.Language.Support.Diagnose(SupportRequest.Empty, code, s_errorsOnly);

            Assert.AreEqual(1, diagnostics.Count());
            Diagnostic first = diagnostics.First();
            Assert.AreEqual("E:superchecker", first.Id);
            Assert.AreEqual(7, first.Reference.Position.LineNumber);
            Assert.AreEqual("\"GO_FilterPrevious\" class is missing super().__init__() in its initializer for base class \"Rhino.Input.Custom.GetObject\"", first.Message);
        }

        [Test]
        public void TestPython3_Complete_SkipBlockComments()
        {
            const string P = "#";

            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
$@"
import os
import os as aa
import os as bb
import os as cc
import os as dd
import rhinoscriptsyntax as RS

{P} os is correct
os.

{P} aa:
{P} should complete as if it is 'os'
""""""
import rhinoscriptsyntax as aa
""""""
aa.

{P} bb:
{P} should complete as if it is 'os'
s = 42 # import rhinoscriptsyntax as bb
bb.

{P} cc:
{P} should complete as if it is 'os'
s = 'import rhinoscriptsyntax as cc'
cc.


{P} dd:
{P} should complete as if it is 'os'
'''
import rhinoscriptsyntax as dd
'''
dd.


RS.
");

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            // os.
            completions = support.Complete(SupportRequest.Empty, code, 135, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
            Assert.IsTrue(completions.Any(c => c.Text == "environ"));

            // aa.
            completions = support.Complete(SupportRequest.Empty, code, 227, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
            Assert.IsTrue(completions.Any(c => c.Text == "environ"));

            // bb.
            completions = support.Complete(SupportRequest.Empty, code, 318, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
            Assert.IsTrue(completions.Any(c => c.Text == "environ"));

            // cc.
            completions = support.Complete(SupportRequest.Empty, code, 406, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
            Assert.IsTrue(completions.Any(c => c.Text == "environ"));

            // dd.
            completions = support.Complete(SupportRequest.Empty, code, 500, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
            Assert.IsTrue(completions.Any(c => c.Text == "environ"));

            // RS.
            completions = support.Complete(SupportRequest.Empty, code, 509, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);
            Assert.IsTrue(completions.Any(c => c.Text == "AddAlias"));
        }

        [Test]
        public void TestPython3_CompleteSignature_ParameterIndex_Nested()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82584 Signature has wrong param index
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject( (1,2,3), ");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
#if RC8_15
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteSignatureOptions.Empty);
#else
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#endif

            Assert.AreEqual(2, signatures.Count());

            SignatureInfo sig;

            sig = signatures.ElementAt(0);
            Assert.AreEqual(1, sig.ParameterIndex);

            sig = signatures.ElementAt(1);
            Assert.AreEqual(1, sig.ParameterIndex);
        }

        [Test]
        public void TestPython3_CompleteSignature_ParameterIndex_NestedFunction()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82584 Signature has wrong param index
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import os.path as op
import Rhino

Rhino.Input.RhinoGet.GetOneObject(op.dirname(""test""),");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
#if RC8_15
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteSignatureOptions.Empty);
#else
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#endif

            Assert.AreEqual(2, signatures.Count());

            SignatureInfo sig;

            sig = signatures.ElementAt(0);
            Assert.AreEqual(1, sig.ParameterIndex);

            sig = signatures.ElementAt(1);
            Assert.AreEqual(1, sig.ParameterIndex);
        }

        [Test]
        public void TestPython3_PIP_InstallWithDependencies()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82730
            ILanguage py3 = GetLanguage(LanguageSpec.Python3);

            IEnviron environ = py3.Environs.CreateEnviron($"{SetupFixture.RHINOCODE_PYTHON_VENV_PREFIX}install_jaxcpu");

            IPackage pkg = environ.AddPackage(new PackageSpec("jax[cpu]"));
            Assert.AreEqual("jax", pkg.Id);
            Assert.IsTrue(environ.Contains(new PackageSpec("jax")));
        }
#endif

#if RC8_11
        [Test]
        public void TestPython3_Library()
        {
            ILanguage python3 = GetLanguage(LanguageSpec.Python3);

            TryGetTestFilesPath(out string fileDir);
            ILanguageLibrary library = python3.CreateLibrary(new Uri(Path.Combine(fileDir, "py3", "test_library")));

            ICode code;

            var utils = new LibraryPath(new Uri("utils/", UriKind.Relative));
            Assert.IsNotEmpty(library.GetCodes(utils));

            code = library.GetCodes().First(c => c.Title == "__init__.py");
            Assert.IsTrue(LanguageSpec.Python3.Matches(code.LanguageSpec));

            code = library.GetCodes().First(c => c.Title == "riazi.py");
            Assert.IsTrue(LanguageSpec.Python3.Matches(code.LanguageSpec));

            code = library.GetCodes().First(c => c.Title == "someData.json");
            Assert.IsTrue(LanguageSpec.JSON.Matches(code.LanguageSpec));
        }

        [Test]
        public void TestPython3_DebugDisconnects()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83214
            // python 3 debugger does not stop on 'pass' statements
            // so using Test() instead
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
value = None
def Test(v):
    global value
    value = v

def First():
    Test(0) # line 8
    Test(42) # line 9

First()
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 8), DebugAction.Disconnect);
            controls.DoNotExpectPause(new CodeReferenceBreakpoint(code, 9));

            var ctx = new DebugContext
            {
                AutoApplyParams = true,
                Outputs = { ["value"] = default }
            };
            code.DebugControls = controls;
            code.Debug(ctx);

            Assert.True(controls.Pass);
            Assert.IsTrue(ctx.Outputs.Get<int>("value") == 42);
        }

        [Test]
        public void TestPython3_StructInitAllKwargs()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83233
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import Rhino

torusA = Rhino.Geometry.Torus(
    basePlane=Rhino.Geometry.Plane.WorldXY,
    majorRadius=20.0,
    minorRadius=10.0)

a = torusA.IsValid

torusB = Rhino.Geometry.Torus(
    Rhino.Geometry.Plane.WorldXY,
    majorRadius=20.0,
    minorRadius=10.0)

b = torusB.IsValid
");

            var ctx = new RunContext
            {
                AutoApplyParams = true,
                Outputs = { ["a"] = false, ["b"] = false }
            };
            code.Run(ctx);

            Assert.IsTrue(ctx.Outputs.Get<bool>("a"));
            Assert.IsTrue(ctx.Outputs.Get<bool>("b"));
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_IndentWhiteSpace()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""
a = ""Hello Python 3 in Grasshopper!""
print(a)

");

            // NOTE:
            // force whitespace indentation when converting to scriptinstance
            script.ConvertToScriptInstance(addSolve: false, addPreview: false, new FormatOptions { IndentWithSpaces = true });

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 3
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        a = ""Hello Python 3 in Grasshopper!""
        print(a)
        
        
        return
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_IndentTabs()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""
a = ""Hello Python 3 in Grasshopper!""
print(a)

");

            // NOTE:
            // force tab indentation when converting to scriptinstance
            script.ConvertToScriptInstance(addSolve: false, addPreview: false, new FormatOptions { IndentWithSpaces = false });

            // NOTE:
            // no params are defined so RunScript() is empty
            // !! string literal has tab indents !!
            Assert.AreEqual(@"#! python 3
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
	def RunScript(self):
		a = ""Hello Python 3 in Grasshopper!""
		print(a)
		
		
		return
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_IndentPreferredTab()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""
a = ""Hello Python 3 in Grasshopper!""
print(a)

def TestIndent():
	print(""indent is tab"")
	pass
");

            // NOTE:
            // force whitespace when converting to scriptinstance.
            // the script already has tab indentation and that should prevail
            script.ConvertToScriptInstance(addSolve: false, addPreview: false, new FormatOptions { IndentWithSpaces = true });

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 3
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
	def RunScript(self):
		a = ""Hello Python 3 in Grasshopper!""
		print(a)
		
		return


def TestIndent():
	print(""indent is tab"")
	pass
", script.Text);
        }

        [Test]
        public void TestPython3_ScriptInstance_Convert_IndentPreferredWhiteSpace()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 3
""""""Grasshopper Script""""""
a = ""Hello Python 3 in Grasshopper!""
print(a)

def TestIndent():
  print(""indent is 2 spaces"")
  pass
");

            // NOTE:
            // force tab when converting to scriptinstance.
            // the script already has 2-space indentation and that should prevail
            script.ConvertToScriptInstance(addSolve: false, addPreview: false, new FormatOptions { IndentWithSpaces = false });

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 3
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
  def RunScript(self):
    a = ""Hello Python 3 in Grasshopper!""
    print(a)
    
    return


def TestIndent():
  print(""indent is 2 spaces"")
  pass
", script.Text);
        }

        [Test]
        public void TestPython3_DebugPauses_ScriptInstance()
        {
            const string INSTANCE = "__instance__";

            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
$@"
class Script_Instance:
    def RunScript(self, x, y):
        __pynet_sys__._getframe(0).f_trace = __pynet_tracefunc__
        __pynet_sys__.settrace(__pynet_tracefunc__)
        return x + y # line 6

{INSTANCE} = Script_Instance()
");

            using DebugContext instctx = new()
            {
                AutoApplyParams = true,
                Options = { ["python.keepScope"] = true },
                Outputs = { [INSTANCE] = default }
            };
            code.Run(instctx);
            dynamic instance = instctx.Outputs.Get(INSTANCE);

            var breakpoint = new CodeReferenceBreakpoint(code, 6);
            var controls = new DebugPauseDetectControls(breakpoint);
            code.DebugControls = controls;

            int result = 0;
            using DebugContext ctx = new();
            using DebugGroup g = code.DebugWith(ctx);
            result = (int)instance.RunScript(21, 21);

            Assert.True(controls.Pass);
            Assert.AreEqual(42, result);
        }

        [Test]
        public void TestPython3_DebugExpand_NoError()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83942
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
$@"
m = [42, 43]
print(m) # line 3
");

            bool noExceptThrown = true;
            var breakpoint = new CodeReferenceBreakpoint(code, 3);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("m"),
            });
            controls.OnReceivedExpected += (ExecVariable v) =>
            {
                try
                {
                    v.Expand();
                }
                catch (Exception)
                {
                    noExceptThrown = false;
                }

                return true;
            };
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(noExceptThrown);
            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_DebugExpand_ErrorsLargeInt()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83942
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
$@"
m = [38675871645874365347856, 2587630952649356034856]
print(m) # line 3
");

            bool exceptThrown = false;
            bool exceptMatched = false;
            var breakpoint = new CodeReferenceBreakpoint(code, 3);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("m"),
            });
            controls.OnReceivedExpected += (ExecVariable v) =>
            {
                try
                {
                    v.Expand();
                }
                catch (Exception ex)
                {
                    exceptThrown = true;
                    exceptMatched = ex.Message.Contains("Python int too large to convert to C ssize_t in method Void .ctor");
                }

                return true;
            };
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(exceptThrown && exceptMatched);
            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_TextFlagLookup()
        {
            const string P = "#";
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
$@"
{P} flag: python.keepScope
{P} flag: grasshopper.inputs.marshaller.asStructs
import os
");

            var ctx = new RunContext();
            code.Run(ctx);

            Assert.IsTrue(ctx.Options.Get("python.keepScope", false));
            Assert.IsTrue(ctx.Options.Get("grasshopper.inputs.marshaller.asStructs", false));
        }
#endif

#if RC8_12
        [Test]
        public void TestPython3_Threaded_ExclusiveStreams()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode("print(a, b)");

            string[] outputs = RunManyExclusiveStreams(code, 3);

            Assert.AreEqual("21 21\n", outputs[0]);
            Assert.AreEqual("22 22\n", outputs[1]);
            Assert.AreEqual("23 23\n", outputs[2]);
        }
#endif


#if RC8_14
        [Test]
        public void TestPython3_CompleteSignature_GH_CurveXCurve()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84661
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import ghpythonlib.components as comps
comps.CurveXCurve(");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
#if RC8_15
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteSignatureOptions.Empty);
#else
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#endif

            Assert.AreEqual(1, signatures.Count());

            SignatureInfo sig;

            sig = signatures.ElementAt(0);
            Assert.AreEqual(0, sig.ParameterIndex);

            Assert.AreEqual("CurveXCurve(*args, **kwargs)", sig.Text);

            Assert.AreEqual(@"
Solve intersection events for two curves.
Input:
	curve_a [Curve] - First curve
	curve_b [Curve] - Second curve
Returns:
	points [Point] - Intersection events
	params_a [Number] - Parameters on first curve
	params_b [Number] - Parameters on second curve".Replace(Environment.NewLine, "\n"), sig.Description);

        }

        [Test]
        public void TestPython3_CompleteSignature_GH_CurveXCurve_FirstArg()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84661
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import ghpythonlib.components as comps
comps.CurveXCurve(arg,");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
#if RC8_15
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteSignatureOptions.Empty);
#else
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);
#endif

            Assert.AreEqual(1, signatures.Count());

            SignatureInfo sig;

            sig = signatures.ElementAt(0);
            Assert.AreEqual(1, sig.ParameterIndex);

            Assert.AreEqual("CurveXCurve(*args, **kwargs)", sig.Text);

            Assert.AreEqual(@"
Solve intersection events for two curves.
Input:
	curve_a [Curve] - First curve
	curve_b [Curve] - Second curve
Returns:
	points [Point] - Intersection events
	params_a [Number] - Parameters on first curve
	params_b [Number] - Parameters on second curve".Replace(Environment.NewLine, "\n"), sig.Description);

        }
#endif

#if RC8_15
        [Test]
        public void TestPython3_DebugSelf()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81598
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
class F:
    def Test(self):
        print() # line 4

f = F()
f.Test()
");

            var breakpoint = new CodeReferenceBreakpoint(code, 4);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("self"),
            });

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_DebugSelf_TypeParameter()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81598
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
class F:
    class_test = 42

    def Test(self):
        print() # line 6

f = F()
f.Test()
");

            var breakpoint = new CodeReferenceBreakpoint(code, 6);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("self"),
            })
            {
                OnReceivedExpected = (v) =>
                {
                    if (GetIdentifier(v) == "self")
                    {
                        ExecVariable[] members = v.Expand().ToArray();
                        Assert.IsTrue(members.Any(m => GetIdentifier(m) == "class_test"));
                    }

                    return true;
                }
            };

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython3_DebugSelf_InstanceParameter()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81598
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
class F:
    def __init__(self):
        self.test = 42

    def Test(self):
        print() # line 7

f = F()
f.Test()
");

            var breakpoint = new CodeReferenceBreakpoint(code, 7);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("self"),
            })
            {
                OnReceivedExpected = (v) =>
                {
                    if (GetIdentifier(v) == "self")
                    {
                        ExecVariable[] members = v.Expand().ToArray();
                        Assert.IsTrue(members.Any(m => GetIdentifier(m) == "test"));
                    }

                    return true;
                }
            };

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }
#endif

#if RC8_15
        [Test]
        public void TestPython3_PIP_INI_Exists()
        {
            string defaultEnvLocation = GetLanguage(LanguageSpec.Python3).Environs.OfIdentity("default").Location;
            // e.g. ~/.rhinocode/py39-rh8/
            string cwd = Path.GetDirectoryName(Path.GetDirectoryName(defaultEnvLocation));
            string pipini = Path.Combine(cwd, "pip.ini");

            Assert.True(File.Exists(pipini));
        }

        [Test]
        public void TestPython3_ExecSpec_Platform_First()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
# platform: rhino3d@8
# platform: revit 2023
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetPlatformSpec(out PlatformSpec pspec));
            Assert.AreEqual(new PlatformSpec("*.*.rhino3d", "8.*.*"), pspec);
        }

        [Test]
        public void TestPython3_ExecSpec_Platform_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
# platform: 
# platform: rhino3d@8
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetPlatformSpec(out PlatformSpec pspec));
            Assert.AreEqual(new PlatformSpec("*.*.rhino3d", "8.*.*"), pspec);
        }

        [Test]
        public void TestPython3_ExecSpec_Async_First()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
# async: true
# async: false
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

#if RC9_0
            Assert.True(execSpec.TryGetAsync(out bool isAsync));
            Assert.True(isAsync);
#else
            Assert.True(execSpec.TryGetAsync(out bool? isAsync));
            Assert.True(isAsync ?? false);
#endif
        }

        [Test]
        public void TestPython3_ExecSpec_Async_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
# async: maybe
# async: true
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

#if RC9_0
            Assert.True(execSpec.TryGetAsync(out bool isAsync));
            Assert.True(isAsync);
#else
            Assert.True(execSpec.TryGetAsync(out bool? isAsync));
            Assert.True(isAsync ?? false);
#endif
        }

        [Test]
        public void TestPython3_ExecSpec_EnvironId_First()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
# venv: first_custom
# venv: second_custom
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetEnvironId(out string environid));
            Assert.AreEqual("first_custom", environid);
        }

        [Test]
        public void TestPython3_ExecSpec_EnvironId_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
# venv: 
# venv: first_custom
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetEnvironId(out string environid));
            Assert.AreEqual("first_custom", environid);
        }

        [Test]
        public void TestPython3_Flags_Defaults()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode();

            ReadOnlyContextOptions cdefautls = code.GetContextOptionsDefaults();
            foreach (string key in cdefautls)
            {
                Assert.False(cdefautls.Get<bool>(key));
            }
        }

        [Test]
        public void TestPython3_Flags()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
# flag: pythonnet.pureScope
# flag: python.reloadEngine
# flag: python.keepScope
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetContextOptions(out ReadOnlyContextOptions opts));
            Assert.True(opts.Get("pythonnet.pureScope", false));
            Assert.True(opts.Get("python.reloadEngine", false));
            Assert.True(opts.Get("python.keepScope", false));
        }

        [Test]
        public void TestPython3_Hover_Empty()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85077
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
import os
jack : int = 42");

            IEnumerable<HoverInfo> hovers =
                code.Language.Support.Hover(SupportRequest.Empty, code, 1, HoverOptions.Empty);

            Assert.IsEmpty(hovers);
        }

        [Test]
        public void TestPython3_Hover_Int()
        {
            string s = @"
import os
jac";
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85077
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(s + "k : int = 42");

            IEnumerable<HoverInfo> hovers =
                code.Language.Support.Hover(SupportRequest.Empty, code, s.Length, HoverOptions.Empty);

            Assert.IsNotEmpty(hovers);
            Assert.AreEqual("int", hovers.First().Text);
        }
#endif

#if RC8_16
        [Test]
        public void TestPython3_ThreadSafeScope()
        {
            const int THREAD_COUNT = 5;
            const int THREAD_CHECK_COUNT = 20;
            const string INP_NAME = "__inp__";
            const string INP_CHECK_NAME = "__inp_check__";
            const string OUT_NAME = "__out__";
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode($@"
import sys
sys.setswitchinterval(1)
import time

for m in range({THREAD_CHECK_COUNT}):
    time.sleep(0.100)
    {INP_CHECK_NAME}({INP_NAME})

{OUT_NAME} = 42 + {INP_NAME}
");

            code.Inputs.Add(INP_NAME);
            code.Inputs.Add(INP_CHECK_NAME);
            code.Outputs.Add(OUT_NAME);

            // NOTE:
            // setting ["python.keepScope"] = true
            // will make this code fail and cause race condition

            using RunGroup group = code.RunWith("test");
            int counter = 0;
            Parallel.For(0, THREAD_COUNT, (i) =>
            {
                int checkcounter = 0;
                var ctx = new RunContext($"Thread {i}")
                {
                    Inputs = { [INP_NAME] =  i,
                                [INP_CHECK_NAME] = (int index) => {
                                    Assert.AreEqual(i, index);
                                    checkcounter++;
                                }
                    },
                    Outputs = { [OUT_NAME] = -1 },
                };

                code.Run(ctx);

                Assert.IsTrue(ctx.Outputs.TryGet(OUT_NAME, out int value));
                Assert.AreEqual(42 + i, value);

                Assert.AreEqual(THREAD_CHECK_COUNT, checkcounter);

                Interlocked.Increment(ref counter);
            });

            Assert.AreEqual(THREAD_COUNT, counter);
        }

        [Test]
        public void TestPython3_DebugTracing_StackWatch_L1()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
            $@"
");
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                new (StackActionKind.Pushed, ExecEvent.Call, 1, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 1, ExecEvent.Line, 1),
                new (StackActionKind.Swapped, ExecEvent.Line, 1, ExecEvent.Return, 1)
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        [Test]
        public void TestPython3_DebugTracing_StackWatch_Function_L2()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
            $@"
def L1():
    pass
L1()
");
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                // python defining L1()
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                // python running  L1()
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 4),
                // python entering level 2
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 3),
                new (StackActionKind.Swapped, ExecEvent.Line, 3, ExecEvent.Return, 3),
                // python returning to level 1
                new (StackActionKind.Swapped, ExecEvent.Line, 4, ExecEvent.Return, 4)
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        [Test]
        public void TestPython3_DebugTracing_StackWatch_Function_L2_Class()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
            $@"
class D:
    pass
def L1():
    pass
L1()
");
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                // python defining class D
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 3),
                new (StackActionKind.Swapped, ExecEvent.Line, 3, ExecEvent.Return, 3),
                // python defining class L1()
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 4),
                // python running L1()
                new (StackActionKind.Swapped, ExecEvent.Line, 4, ExecEvent.Line, 6),
                // python entering L1()
                new (StackActionKind.Pushed, ExecEvent.Call, 4, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 4, ExecEvent.Line, 5),
                new (StackActionKind.Swapped, ExecEvent.Line, 5, ExecEvent.Return, 5),
                // return back to level 1
                new (StackActionKind.Swapped, ExecEvent.Line, 6, ExecEvent.Return, 6),
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        [Test]
        public void TestPython3_DebugTracing_StackWatch_Function_L3()
        {
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
            $@"
def L2():
    pass
def L1():
    L2()
L1()
");
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                // defining L2()
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                // defining L1()
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 4),
                // python running  L1()
                new (StackActionKind.Swapped, ExecEvent.Line, 4, ExecEvent.Line, 6),
                // python entering level 2
                new (StackActionKind.Pushed, ExecEvent.Call, 4, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 4, ExecEvent.Line, 5),
                // python entering level 3
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 3),
                new (StackActionKind.Swapped, ExecEvent.Line, 3, ExecEvent.Return, 3),
                // python returning to level 2
                new (StackActionKind.Swapped, ExecEvent.Line, 5, ExecEvent.Return, 5),
                // python returning to level 1
                new (StackActionKind.Swapped, ExecEvent.Line, 6, ExecEvent.Return, 6)
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        [Test]
        public void TestPython3_DebugTracing_Pass_Middle()
        {

            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
            $@"
import sys
def Foo():
    pass
def pass_in_middle():       # CALL 5
    Foo()                   # LINE 6
    pass                    # does not stop here
    Foo()                   # LINE 8
pass_in_middle()            # LINE 9
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 9, ExecEvent.Line, DebugAction.StepIn),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                // not stopping on pass on line 7
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 9, ExecEvent.Return, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 9));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"{step.Line} !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        [Test]
        public void TestPython3_Complete_GenericTypes()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85354
            Code code = GetLanguage(LanguageSpec.Python3).CreateCode(
@"
from system.Collection.Generic import ");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

            Assert.IsNotEmpty(completions);
            Assert.IsEmpty(completions.Where(c => c.Text.Contains('`')));
        }
#endif

        static string GetIdentifier(ExecVariable variable)
        {
#if RC8_16
            return variable.Id.Identifier;
#else
            return variable.Id;
#endif
        }

        static DiagnoseOptions s_errorsOnly = new() { Errors = true, Hints = false, Infos = false, Warnings = false };
        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py3\", "test_*.py");
    }
}
