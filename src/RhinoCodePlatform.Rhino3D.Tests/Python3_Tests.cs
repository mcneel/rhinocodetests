using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Testing;

using RhinoCodePlatform.Rhino3D.Languages;

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
                if (ex.Diagnostics.First().Reference.Position.LineNumber != 6)
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
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepOver);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 7));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);

            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepOver);

            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }
#endif

#if RC8_9
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
#endif

        [Test]
        public void TestPython3_Complete_RhinoScriptSyntax()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"
import rhinoscriptsyntax as rs
rs.");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length);

            Assert.True(completions.Any(c => c.Text == "test_class_method"));
        }

#if RC8_9
        [Test]
        public void TestPython3_CompleteSignature()
        {
            Code code = GetLanguage(this, LanguageSpec.Python3).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject(");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length);

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
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length);

            Assert.AreEqual(2, signatures.Count());

            SignatureInfo sig;

            sig = signatures.ElementAt(0);
            Assert.AreEqual(1, sig.ParameterIndex);

            sig = signatures.ElementAt(1);
            Assert.AreEqual(1, sig.ParameterIndex);
        }
#endif

        [Test]
        public void TestPython3_PIP_SitePackage()
        {
            // RH-81895
            string pkgPath = string.Empty;

            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);
            Code code = py3.CreateCode(
$@"
# venv: site-packages
# r: rx
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
            // RH-81895
            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);
            Code code = py3.CreateCode(
@"
# venv: site-packages
# r: fpdf
import fpdf
");

            string pkgPath = string.Empty;
            RunContext ctx = GetRunContext();
            code.Run(ctx);

            code = py3.CreateCode(
$@"
# r: fpdf
import fpdf

{nameof(pkgPath)} = fpdf.__file__
");

            ctx = GetRunContext();
            ctx.Outputs.Set(nameof(pkgPath), pkgPath);

            code.Run(ctx);

            pkgPath = ctx.Outputs.Get<string>(nameof(pkgPath));
            Assert.True(new Regex(@"[Ll]ib[\\/]site-packages[\\/]").IsMatch(pkgPath));
        }

#if RC8_9
        [Test]
        public void TestPython3_PIP_AccessDeniedError()
        {
            // https://github.com/mcneel/rhino/pull/72450
            ILanguage py3 = GetLanguage(this, LanguageSpec.Python3);

            if (py3.Environs.OfIdentity("test_access_denied") is IEnviron testEnviron)
            {
                py3.Environs.DeleteEnviron(testEnviron);
            }

            py3.CreateCode(
@"
# venv: test_access_denied
# r: openexr
import OpenEXR
").Run(GetRunContext());


            Code code = py3.CreateCode(
$@"
# venv: test_access_denied
# r: openexr-tools
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

            if (py3.Environs.OfIdentity("test_pip_install_requests") is IEnviron testEnviron)
            {
                py3.Environs.DeleteEnviron(testEnviron);
            }

            IEnviron environ = py3.Environs.CreateEnviron("test_pip_install_requests");

            IPackage pkg = environ.AddPackage(new PackageSpec("requests", "2.31.0"));
            Assert.AreEqual("requests==2.31.0 (Any)", pkg.ToString());
            Assert.AreEqual("requests", pkg.Id);
            Assert.AreEqual("2.31.0", pkg.Version.ToString());
        }
#endif

#if RC8_9
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
#endif

        static DiagnoseOptions s_errorsOnly = new DiagnoseOptions { Errors = true, Hints = false, Infos = false, Warnings = false };
        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py3\", "test_*.py");
    }
}
