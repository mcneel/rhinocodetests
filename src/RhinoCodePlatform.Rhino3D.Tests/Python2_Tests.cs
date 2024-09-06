using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
using Rhino.Runtime.Code.Testing;

#if RC8_11
using RhinoCodePlatform.Rhino3D.Languages.GH1;
#else
using RhinoCodePlatform.Rhino3D.Languages;
#endif

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Python2_Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython2_Script(ScriptInfo scriptInfo)
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
        public void TestPython2_Compile_Script()
        {
            // assert throws compile exception on run/debug/profile
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_RuntimeErrorLine_InScript()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_DebugStop()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
@"
import sys
print(sys)  # line 3
");

            var breakpoint = new CodeReferenceBreakpoint(code, 3);
            var controls = new DebugStopperControls(breakpoint);

            var ctx = new DebugContext();

            code.DebugControls = controls;


            Assert.Throws<DebugStopException>(() => code.Debug(ctx));
        }

#if RC8_9
        [Test]
        public void TestPython2_DebugPauses_Script_StepOver()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
@"
def First():
    pass # line 3
    pass # line 4

First()
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 3), DebugAction.StepOver);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 4));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython2_DebugPauses_Script_StepOut()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
@"
def First():
    pass # line 3
    pass # line 4

First()
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 3), DebugAction.StepOut);
            controls.DoNotExpectPause(new CodeReferenceBreakpoint(code, 4));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestPython2_Complete_RhinoScriptSyntax()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
            result &= CompletionKind.Method == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "rhapp");
            result &= CompletionKind.Module == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython2_Complete_RhinoCommon_Rhino()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_Complete_StdLib_os()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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

            cinfo = completions.First(c => c.Text == "abort");
            result &= CompletionKind.Method == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "environ");
            result &= CompletionKind.Method == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython2_Complete_StdLib_os_path()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
            result &= CompletionKind.Method == cinfo.Kind;

            cinfo = completions.First(c => c.Text == "curdir");
            result &= CompletionKind.Method == cinfo.Kind;

            Assert.True(result);
        }

        [Test]
        public void TestPython2_Complete_Import()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
@"
import ");

            string text = code.Text;
            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

            Assert.IsNotEmpty(completions);
        }

        // https://mcneel.myjetbrains.com/youtrack/issue/RH-81189
        [Test]
        public void TestPython2_Complete_LastIndex()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InCommentBlock()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InCommentBlock_Nested()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InCommentBlock_Start()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
@"""""""
");

            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, 3, CompleteOptions.Empty);

            Assert.IsEmpty(completions);
        }

        [Test]
        public void TestPython2_CompleteNot_InFunctionDocstring()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InLiteralString_Double()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InLiteralString_Single()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InLiteralString_DoubleEscaped()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InLiteralString_SingleEscaped()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteSignature()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
                @"GetOneObject(prompt: str, acceptNothing: bool, filter: ObjectType) -> (Result, ObjRef)",
                sig.Text
                );
            Assert.AreEqual("prompt: str", sig.Parameters[0].Name);
            Assert.AreEqual("acceptNothing: bool", sig.Parameters[1].Name);
            Assert.AreEqual("filter: ObjectType", sig.Parameters[2].Name);

            sig = signatures.ElementAt(1);
            Assert.AreEqual(0, sig.ParameterIndex);
            Assert.AreEqual(
                @"GetOneObject(prompt: str, acceptNothing: bool, filter: GetObjectGeometryFilter) -> (Result, ObjRef)",
                sig.Text
                );
            Assert.AreEqual("prompt: str", sig.Parameters[0].Name);
            Assert.AreEqual("acceptNothing: bool", sig.Parameters[1].Name);
            Assert.AreEqual("filter: GetObjectGeometryFilter", sig.Parameters[2].Name);
        }

        [Test]
        public void TestPython2_CompleteSignature_ParameterIndex()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
@"
import Rhino
Rhino.Input.RhinoGet.GetOneObject(prompt, ");

            string text = code.Text;
            IEnumerable<SignatureInfo> signatures =
                code.Language.Support.CompleteSignature(SupportRequest.Empty, code, text.Length, CompleteOptions.Empty);

            Assert.AreEqual(2, signatures.Count());

            SignatureInfo first = signatures.ElementAt(0);
            Assert.AreEqual(1, first.ParameterIndex);

            SignatureInfo second = signatures.ElementAt(1);
            Assert.AreEqual(1, second.ParameterIndex);
        }

        [Test]
        public void TestPython2_ScriptInstance_Convert()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
""""""Grasshopper Script""""""
a = ""Hello Python 2 in Grasshopper!""
print(a)

");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 2
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        a = ""Hello Python 2 in Grasshopper!""
        print(a)
        
        
        return
", script.Text);
        }

        [Test]
        public void TestPython2_ScriptInstance_Convert_LastEmptyLine()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
print(a)");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 2
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
        public void TestPython2_ScriptInstance_Convert_CommentBlock()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
""""""Grasshopper Script

Hello Python 2 in Grasshopper!
Hello Python 2 in Grasshopper!

Hello Python 2 in Grasshopper!

""""""
a = ""Hello Python 2 in Grasshopper!""
print(a)

");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 2
""""""Grasshopper Script

Hello Python 2 in Grasshopper!
Hello Python 2 in Grasshopper!

Hello Python 2 in Grasshopper!

""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        a = ""Hello Python 2 in Grasshopper!""
        print(a)
        
        
        return
", script.Text);
        }

        [Test]
        public void TestPython2_ScriptInstance_Convert_WithFunction()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
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
            Assert.AreEqual(@"#! python 2
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
        public void TestPython2_ScriptInstance_Convert_AddSolveOverrides()
        {
            const string P = "#";
            var script = new Grasshopper1Script(@"#! python 2
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: false);

            Assert.AreEqual($@"#! python 2
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
        public void TestPython2_ScriptInstance_Convert_AddPreviewOverrides()
        {
            const string P = "#";
            var script = new Grasshopper1Script(@"#! python 2
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: false, addPreview: true);

            Assert.AreEqual($@"#! python 2
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
        public void TestPython2_ScriptInstance_Convert_AddBothOverrides()
        {
            const string P = "#";
            var script = new Grasshopper1Script(@"#! python 2
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: true);

            Assert.AreEqual($@"#! python 2
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
        public void TestPython2_ScriptInstance_Convert_AddBothOverrides_Steps()
        {
            const string P = "#";
            var script = new Grasshopper1Script(@"#! python 2
import System
import Rhino
import Grasshopper

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: false);
            script.ConvertToScriptInstance(addSolve: false, addPreview: true);

            Assert.AreEqual($@"#! python 2
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

#if RC8_10
        [Test]
        public void TestPython2_Complete_SkipBlockComments()
        {
            const string P = "#";

            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteSignature_ParameterIndex_Nested()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82584 Signature has wrong param index
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
#endif

#if RC8_11
        [Test]
        public void TestPython2_Library()
        {
            ILanguage python2 = GetLanguage(this, LanguageSpec.Python2);

            TryGetTestFilesPath(out string fileDir);
            LanguageLibrary library = python2.CreateLibrary(new Uri(Path.Combine(fileDir, "py2", "test_library")));

            ICode code;

            code = library.GetCodes().First(c => c.Title == "__init__.py");
            Assert.IsTrue(LanguageSpec.Python2.Matches(code.LanguageSpec));

            code = library.GetCodes().First(c => c.Title == "riazi.py");
            Assert.IsTrue(LanguageSpec.Python2.Matches(code.LanguageSpec));

            code = library.GetCodes().First(c => c.Title == "someData.json");
            Assert.IsTrue(LanguageSpec.JSON.Matches(code.LanguageSpec));
        }

        [Test]
        public void TestPython2_DebugDisconnects()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83214
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_ScriptInstance_Convert_IndentWhiteSpace()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
""""""Grasshopper Script""""""
a = ""Hello Python 2 in Grasshopper!""
print(a)

");

            // NOTE:
            // force whitespace indentation when converting to scriptinstance
            script.ConvertToScriptInstance(addSolve: false, addPreview: false, new FormatOptions { IndentWithSpaces = true });

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"#! python 2
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        a = ""Hello Python 2 in Grasshopper!""
        print(a)
        
        
        return
", script.Text);
        }

        [Test]
        public void TestPython2_ScriptInstance_Convert_IndentTabs()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
""""""Grasshopper Script""""""
a = ""Hello Python 2 in Grasshopper!""
print(a)

");

            // NOTE:
            // force tab indentation when converting to scriptinstance
            script.ConvertToScriptInstance(addSolve: false, addPreview: false, new FormatOptions { IndentWithSpaces = false });

            // NOTE:
            // no params are defined so RunScript() is empty
            // !! string literal has tab indents !!
            Assert.AreEqual(@"#! python 2
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
	def RunScript(self):
		a = ""Hello Python 2 in Grasshopper!""
		print(a)
		
		
		return
", script.Text);
        }

        [Test]
        public void TestPython2_ScriptInstance_Convert_IndentPreferredTab()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
""""""Grasshopper Script""""""
a = ""Hello Python 2 in Grasshopper!""
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
            Assert.AreEqual(@"#! python 2
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
	def RunScript(self):
		a = ""Hello Python 2 in Grasshopper!""
		print(a)
		
		return


def TestIndent():
	print(""indent is tab"")
	pass
", script.Text);
        }

        [Test]
        public void TestPython2_ScriptInstance_Convert_IndentPreferredWhiteSpace()
        {
            const string P = "#";
            var script = new Grasshopper1Script($@"
{P}! python 2
""""""Grasshopper Script""""""
a = ""Hello Python 2 in Grasshopper!""
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
            Assert.AreEqual(@"#! python 2
""""""Grasshopper Script""""""
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
  def RunScript(self):
    a = ""Hello Python 2 in Grasshopper!""
    print(a)
    
    return


def TestIndent():
  print(""indent is 2 spaces"")
  pass
", script.Text);
        }

        [Test]
        public void TestPython2_DebugPauses_ScriptInstance()
        {
            const string INSTANCE = "__instance__";

            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
$@"
class Script_Instance:
    def RunScript(self, x, y):
        return x + y # line 4

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

            var breakpoint = new CodeReferenceBreakpoint(code, 4);
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
        public void TestPython2_TextFlagLookup()
        {
            const string P = "#";
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_Threaded_ExclusiveStreams()
        {
            Code code = GetLanguage(this, LanguageSpec.Python2).CreateCode("print(a, b)");

            string[] outputs = RunManyExclusiveStreams(code, 3);

            // NOTE:
            // legacy ironpython print would print (21 21)
            // __future__ print however prints like python 3
            // this is what is expected here since ExclusiveStreams = true
            // forces __future__ print to be wired up in the scope
            Assert.AreEqual("21 21\n", outputs[0]);
            Assert.AreEqual("22 22\n", outputs[1]);
            Assert.AreEqual("23 23\n", outputs[2]);
        }
#endif

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py2\", "test_*.py");
    }
}
