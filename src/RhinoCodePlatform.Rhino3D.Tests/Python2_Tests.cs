using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
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
    public class Python2_Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestPython2_Script(ScriptInfo scriptInfo)
        {
            TestSkip(scriptInfo);

            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(scriptInfo.Uri);

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
        public void TestPython2_Compile_Script()
        {
            // assert throws compile exception on run/debug/profile
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_RuntimeErrorLine_InScript()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_Complete_AfterSingleQuoteComment()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86072
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_Complete_AfterDoubleQuoteComment()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86072
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_CompleteNot_InCommentBlock()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
@"""""""
");

            IEnumerable<CompletionInfo> completions =
                code.Language.Support.Complete(SupportRequest.Empty, code, 3, CompleteOptions.Empty);

            Assert.IsEmpty(completions);
        }

        [Test]
        public void TestPython2_CompleteNot_InFunctionDocstring()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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

            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
#endif

#if RC8_11
        [Test]
        public void TestPython2_Library()
        {
            ILanguage python2 = GetLanguage(LanguageSpec.Python2);

            TryGetTestFilesPath(out string fileDir);
            ILanguageLibrary library = python2.CreateLibrary(new Uri(Path.Combine(fileDir, "py2", "test_library")));

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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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

            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode("print(a, b)");

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

#if RC8_14
        [Test]
        public void TestPython2_CompleteSignature_GH_CurveXCurve()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81419
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84661
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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

            Assert.AreEqual("CurveXCurve()", sig.Text);

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
        public void TestPython2_CompleteSignature_GH_CurveXCurve_FirstArg()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84661
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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

            Assert.AreEqual("CurveXCurve()", sig.Text);

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
        public void TestPython2_ExecSpec_Platform_First()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_ExecSpec_Platform_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_ExecSpec_Async_First()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
@"
# async: true
# async: false
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetAsync(out bool? isAsync));
            Assert.True(isAsync ?? false);
        }

        [Test]
        public void TestPython2_ExecSpec_Async_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
@"
# async: maybe
# async: true
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetAsync(out bool? isAsync));
            Assert.True(isAsync ?? false);
        }

        [Test]
        public void TestPython2_ExecSpec_EnvironId_First()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_ExecSpec_EnvironId_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
        public void TestPython2_Flags_Defaults()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode();

            ReadOnlyContextOptions cdefautls = code.GetContextOptionsDefaults();
            foreach (string key in cdefautls)
            {
                Assert.False(cdefautls.Get<bool>(key));
            }
        }

        [Test]
        public void TestPython2_Flags()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
@"
# flag: python.reloadEngine
# flag: python.keepScope
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetContextOptions(out ReadOnlyContextOptions opts));
            Assert.True(opts.Get("python.reloadEngine", false));
            Assert.True(opts.Get("python.keepScope", false));
        }
#endif

#if RC8_16
        [Test]
        public void TestPython2_ThreadSafeScope()
        {
            const int THREAD_COUNT = 5;
            const int THREAD_CHECK_COUNT = 20;
            const string INP_NAME = "__inp__";
            const string INP_CHECK_NAME = "__inp_check__";
            const string OUT_NAME = "__out__";
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode($@"
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
        public void TestPython2_DebugTracing_StackWatch_Function_L2()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
            $@"
def L1():
    pass
L1()
");

            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 4),
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
        public void TestPython2_DebugTracing_StackWatch_Function_L2_Class()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
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
                // python defining class D
                new (StackActionKind.Pushed, ExecEvent.Call, 3, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 3, ExecEvent.Return, 3),
                // python defining class L1()
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 4),
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
        public void TestPython2_DebugTracing_StackWatch_Function_L3()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
            $@"
def L2():
    pass
def L1():
    L2()
L1()
");

            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                // start - defining L2()
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                // defining L1()
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 4),
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
        public void TestPython2_DebugTracing_StackWatch_L1()
        {
            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
            $@"
");
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                new (StackActionKind.Pushed, ExecEvent.Call, 1, 0, 0),
                // no line 1 event here
                new (StackActionKind.Swapped, ExecEvent.Call, 1, ExecEvent.Return, 1)
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        [Test]
        public void TestPython2_DebugTracing_Pass_Middle()
        {

            Code code = GetLanguage(LanguageSpec.Python2).CreateCode(
            $@"
import sys
def Foo():
    pass
def pass_in_middle():       # CALL 5
    Foo()                   # LINE 6
    pass                    # does stop here
    Foo()                   # LINE 8
pass_in_middle()            # LINE 9
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 9, ExecEvent.Line, DebugAction.StepIn),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
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
#endif

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"py2\", "test_*.py");
    }
}
