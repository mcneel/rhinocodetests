using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Languages;
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
        public void TestPython3_CompileErrorLine_ReturnOutsideFunction()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"
import os

a = None[0
");


            code.DebugControls = new DebugContinueAllControls();

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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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

            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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

            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);
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
            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
                    if (v.Id == "m")
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
                    if (v.Id == "brep_obj")
                    {
                        ExecVariable[] members = v.Expand().ToArray();

                        Assert.IsTrue(members.Any(m => m.Id == "Geometry"));

                        // Guid is not exapandable
                        ExecVariable id = members.First(m => m.Id == "Id");
                        Assert.IsFalse(id.CanExpand);

                        // bool is not exapandable
                        ExecVariable isHidden = members.First(m => m.Id == "IsHidden");
                        Assert.IsFalse(isHidden.CanExpand);

                        // None is not exapandable
                        ExecVariable renderMaterial = members.First(m => m.Id == "RenderMaterial");
                        Assert.IsFalse(isHidden.CanExpand);

                        ExecVariable[] expanded;

                        // assert enumerable with one item has [0] and Count
                        ExecVariable geom = members.First(m => m.Id == "Geometry");
                        ExecVariable edges = geom.Expand().First(m => m.Id == "Edges");
                        expanded = edges.Expand().ToArray();
                        Assert.Greater(expanded.Length, 2);
                        Assert.Contains("[0]", expanded.Select(e => e.Id).ToList());
                        Assert.Contains("Count", expanded.Select(e => e.Id).ToList());

                        // assert array only has one Length member
                        ExecVariable subobjMat = members.First(m => m.Id == "SubobjectMaterialComponents");
                        expanded = subobjMat.Expand().ToArray();
                        Assert.AreEqual(1, expanded.Length);
                        Assert.AreEqual("Length", expanded[0].Id);

                        // assert color is expandable
                        ExecVariable attribs = members.First(m => m.Id == "Attributes");
                        ExecVariable objColor = attribs.Expand().First(m => m.Id == "ObjectColor");
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
        public void TestPython3_CompleteNot_InCommentBlock()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"""""""
");

            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, 3, CompleteOptions.Empty);

            Assert.IsEmpty(completions);
        }

        [Test]
        public void TestPython3_CompleteNot_InFunctionDocstring()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject(");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

            Assert.AreEqual(2, signatures.Count());

            SignatureInfo sig;

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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject(prompt, ");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);

            py3.CreateCode(
$@"
{P} venv: {SetupFixture.RHINOCODE_PYTHON_VENV_PREFIX}access_denied
{P} r: openexr
import OpenEXR
").Run(GetRunContext());


            Code code = py3.CreateCode(
$@"
{P} venv: test_access_denied
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
            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);

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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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

            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            //RH-82584 Signature has wrong param index
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject( (1,2,3), ");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

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
            //RH-82584 Signature has wrong param index
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"
import os.path as op
import Rhino

Rhino.Input.RhinoGet.GetOneObject(op.dirname(""test""),");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

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
            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);

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
            ILanguage python3 = GetLanguage(this, LanguageSpec.Python3);

            TryGetTestFilesPath(out string fileDir);
            LanguageLibrary library = python3.CreateLibrary(new Uri(Path.Combine(fileDir, "py3", "test_library")));

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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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

            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
        public void TestPython3_TextFlagLookup()
        {
            const string P = "#";
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
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
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode("print(a, b)");

            string[] outputs = RunManyExclusiveStreams(code, 3);

            Assert.AreEqual("21 21\n", outputs[0]);
            Assert.AreEqual("22 22\n", outputs[1]);
            Assert.AreEqual("23 23\n", outputs[2]);
        }
#endif

        static DiagnoseOptions s_errorsOnly = new() { Errors = true, Hints = false, Infos = false, Warnings = false };
        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py3\", "test_*.py");
    }
}
