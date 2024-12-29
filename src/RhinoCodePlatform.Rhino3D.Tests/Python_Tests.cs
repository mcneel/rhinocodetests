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
    public class Python_Tests : ScriptFixture
    {
        static IEnumerable<TestCaseData> GetPythons()
        {
            yield return new(LanguageSpec.Python2);
            yield return new(LanguageSpec.Python3);
        }

#if RC8_16
        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_CompileGuard_Specific(LanguageSpec spec)
        {
            int major = RhinoApp.Version.Major;
            int minor = RhinoApp.Version.Minor;

            Code code = GetLanguage(spec).CreateCode($@"
result = False
if __context__.CompileGuards.Contains(""RHINO_{major}_{minor}""):
    result = True
");

            RunContext ctx = GetRunContext(captureStdout: false);

            ctx.AutoApplyParams = true;
            ctx.Outputs["result"] = default;

            Assert.DoesNotThrow(() => code.Run(ctx));
            Assert.True(ctx.Outputs.TryGet("result", out bool data));
            Assert.True(data);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_Complete_ScriptInstance_XformerStack(LanguageSpec spec)
        {
            string s = @"#! python 3
import System
import Rhino.";
            Code code = new Grasshopper1Script(s + @"
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y):
        return
").CreateCode();

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, s.Length, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.Text).ToArray();
            Assert.Contains(nameof(Rhino.Geometry), names);
            Assert.Contains(nameof(Rhino.Display), names);
            Assert.Contains(nameof(Rhino.Runtime), names);
            Assert.Contains(nameof(Rhino.UI), names);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_Complete_ScriptInstance_XformerStack_WithSpace(LanguageSpec spec)
        {
            string s = @"#! python 3
import System
import Rhino.";
            Code code = new Grasshopper1Script(s + @"
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(  Grasshopper.Kernel.GH_ScriptInstance      ):
    def RunScript(self, x, y):
        return
").CreateCode();

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, s.Length, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.Text).ToArray();
            Assert.Contains(nameof(Rhino.Geometry), names);
            Assert.Contains(nameof(Rhino.Display), names);
            Assert.Contains(nameof(Rhino.Runtime), names);
            Assert.Contains(nameof(Rhino.UI), names);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_Complete_ScriptInstance_XformerStack_WithSpace_LegacyComponent(LanguageSpec spec)
        {
            string s = @"#! python 3
import System
import Rhino.";
            Code code = new Grasshopper1Script(s + @"
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(  component      ):
    def RunScript(self, x, y):
        return
").CreateCode();

            IEnumerable<CompletionInfo> completions;
            ISupport support = code.Language.Support;

            completions = support.Complete(SupportRequest.Empty, code, s.Length, CompleteOptions.Empty);
            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.Text).ToArray();
            Assert.Contains(nameof(Rhino.Geometry), names);
            Assert.Contains(nameof(Rhino.Display), names);
            Assert.Contains(nameof(Rhino.Runtime), names);
            Assert.Contains(nameof(Rhino.UI), names);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_ContextTracking(LanguageSpec spec)
        {
            const int THREAD_COUNT = 5;
            const string CID_NAME = "__cid__";
            Code code = GetLanguage(spec).CreateCode($@"
{CID_NAME} = __context__.Id.Id
");

            code.Outputs.Add(CID_NAME);

            using RunGroup group = code.RunWith("test");
            int counter = 0;
            Parallel.For(0, THREAD_COUNT, (i) =>
            {
                var ctx = new RunContext($"Thread {i}")
                {
                    Outputs = { [CID_NAME] = Guid.Empty }
                };

                code.Run(ctx);

                Assert.IsTrue(ctx.Outputs.TryGet(CID_NAME, out Guid cid));
                Assert.AreEqual(ctx.Id.Id, cid);
                Interlocked.Increment(ref counter);
            });

            Assert.AreEqual(THREAD_COUNT, counter);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_ContextTracking_CurrentContext(LanguageSpec spec)
        {
            const int THREAD_COUNT = 5;
            const string CTX_CHECK_NAME = "__ctx_check__";
            Code code = GetLanguage(spec).CreateCode($@"
{CTX_CHECK_NAME}()
");

            code.Inputs.Add(CTX_CHECK_NAME);

            using RunGroup group = code.RunWith("test");
            int counter = 0;
            Parallel.For(0, THREAD_COUNT, (i) =>
            {
                bool checked_context = false;
                var ctx = new RunContext($"Thread {i}");

                ctx.Inputs[CTX_CHECK_NAME] = () =>
                {
                    Assert.AreEqual(ctx.Id, code.ContextTracker.CurrentContext);
                    checked_context = true;
                };

                code.Run(ctx);

                Assert.IsTrue(checked_context);

                Interlocked.Increment(ref counter);
            });

            Assert.AreEqual(THREAD_COUNT, counter);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugForEachLoop_Variable(LanguageSpec spec)
        {
            const string INDEX_VAR = "i";
            const string SUM_VAR = "total";
            Code code = GetLanguage(spec).CreateCode(
$@"
{SUM_VAR} = 0
for {INDEX_VAR} in range(0, 3): # line 3
    {SUM_VAR} += {INDEX_VAR}    # line 4
");

            DebugExpressionVariableResult getIndex(ExecFrame frame)
            {
                return frame.Evaluate().OfType<DebugExpressionVariableResult>().FirstOrDefault(r => r.Value.Id == INDEX_VAR);
            }

            DebugExpressionVariableResult getSum(ExecFrame frame)
            {
                return frame.Evaluate().OfType<DebugExpressionVariableResult>().FirstOrDefault(r => r.Value.Id == SUM_VAR);
            }

            int bp3Counter = -1;
            int bp4Counter = 0;
            var bp3 = new CodeReferenceBreakpoint(code, 3);
            var bp4 = new CodeReferenceBreakpoint(code, 4);

            ExecFrame prevFrame = ExecFrame.Empty;
            var controls = new DebugContinueAllControls();
            controls.Breakpoints.Add(bp3);
            controls.Breakpoints.Add(bp4);
            controls.Paused += (IDebugControls c) =>
            {
                if (c.Results.CurrentThread.CurrentFrame is ExecFrame frame
                        && ExecEvent.Line == frame.Event)
                {
                    if (bp3.Matches(frame))
                    {
                        switch (bp3Counter)
                        {
                            // first arrive at line 3
                            // i does not exist
                            case -1:
                                DebugExpressionVariableResult er = getIndex(frame);
                                Assert.IsNull(er);
                                break;

                            // i = 0
                            case 0:
                                DebugExpressionVariableResult er0 = getIndex(frame);
                                Assert.IsInstanceOf<DebugExpressionVariableResult>(er0);
                                // i does not exist in previous frame
                                Assert.IsFalse(er0.IsModified);
                                Assert.IsTrue(er0.Value.TryGetValue(out int v0));
                                Assert.AreEqual(0, v0);
                                break;

                            // i = 1
                            case 1:
                                DebugExpressionVariableResult er1 = getIndex(frame);
                                er1 = getIndex(prevFrame).WithValue(er1.Value);
                                Assert.IsFalse(er1.IsModified);
                                Assert.IsTrue(er1.Value.TryGetValue(out int v1));
                                Assert.AreEqual(1, v1);

                                DebugExpressionVariableResult ers1 = getSum(frame);
                                ers1 = getSum(prevFrame).WithValue(ers1.Value);
                                Assert.IsTrue(ers1.IsModified);      // sum is modified!
                                Assert.IsTrue(ers1.Value.TryGetValue(out int sum1));
                                Assert.AreEqual(1, sum1);
                                break;

                            // i = 2
                            case 2:
                                DebugExpressionVariableResult er2 = getIndex(frame);
                                er2 = getIndex(prevFrame).WithValue(er2.Value);
                                Assert.IsFalse(er2.IsModified);
                                Assert.IsTrue(er2.Value.TryGetValue(out int v2));
                                Assert.AreEqual(2, v2);

                                DebugExpressionVariableResult ers2 = getSum(frame);
                                ers2 = getSum(prevFrame).WithValue(ers2.Value);
                                Assert.IsTrue(ers2.IsModified);      // sum is modified!
                                Assert.IsTrue(ers2.Value.TryGetValue(out int sum2));
                                Assert.AreEqual(3, sum2);
                                break;

                            // breakpoint 3 will never see i as 3
                            default:
                                Assert.Fail("breakpoint 3 should never see i as 3");
                                break;
                        }
                        bp3Counter++;
                    }

                    else
                    if (bp4.Matches(frame))
                    {
                        switch (bp4Counter)
                        {
                            // first arrive at line 4
                            // i = 0
                            case 0:
                                DebugExpressionVariableResult er0 = getIndex(frame);
                                // python does not stop twice on for loop so
                                // i is not available in frame previous to this
                                Assert.IsFalse(er0.IsModified);
                                Assert.IsTrue(er0.Value.TryGetValue(out int v0));
                                Assert.AreEqual(0, v0);
                                break;

                            // i = 1
                            case 1:
                                DebugExpressionVariableResult er1 = getIndex(frame);
                                er1 = getIndex(prevFrame).WithValue(er1.Value);
                                Assert.IsTrue(er1.IsModified);      // i is modified!
                                Assert.IsTrue(er1.Value.TryGetValue(out int v1));
                                Assert.AreEqual(1, v1);

                                DebugExpressionVariableResult ers0 = getSum(frame);
                                Assert.IsInstanceOf<DebugExpressionVariableResult>(ers0);
                                Assert.IsFalse(ers0.IsModified);
                                Assert.IsTrue(ers0.Value.TryGetValue(out int sum0));
                                Assert.AreEqual(0, sum0);
                                break;

                            // i = 2
                            case 2:
                                DebugExpressionVariableResult er2 = getIndex(frame);
                                er2 = getIndex(prevFrame).WithValue(er2.Value);
                                Assert.IsTrue(er2.IsModified);      // i is modified!
                                Assert.IsTrue(er2.Value.TryGetValue(out int v2));
                                Assert.AreEqual(2, v2);

                                DebugExpressionVariableResult ers1 = getSum(frame);
                                ers1 = getSum(prevFrame).WithValue(ers1.Value);
                                // sum is not modified since it was 1 on entering loop on previous pause
                                Assert.IsFalse(ers1.IsModified);
                                Assert.IsTrue(ers1.Value.TryGetValue(out int sum1));
                                Assert.AreEqual(1, sum1);
                                break;

                            // breakpoint 4 will never see sum as 2
                            default:
                                Assert.Fail("breakpoint 4 should never see sum as 2");
                                break;
                        }
                        bp4Counter++;
                    }

                    prevFrame = frame;
                }
            };
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.AreEqual(-1 + 4, bp3Counter);
            Assert.AreEqual(3, bp4Counter);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StackWatch_L1_Single(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import os
");
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Return, 2)
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_L1(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
def Test():     # LINE 2
    pass        # LINE 3
Test()          # LINE 4
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                    new ( 3, ExecEvent.Line, DebugAction.StepOut),
                new ( 4, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 4));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"Expected: {step.Event} [{step.Line}:] !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_L2(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
def Test2():    # LINE 2
    pass        # LINE 3
def Test():     # LINE 4
    Test2()     # LINE 5
Test()          # LINE 6
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 6, ExecEvent.Line, DebugAction.StepIn),
                    new ( 5, ExecEvent.Line, DebugAction.StepIn),
                        new ( 3, ExecEvent.Line, DebugAction.StepOut),
                    new ( 5, ExecEvent.Return, DebugAction.StepOut),
                new ( 6, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 6));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"Expected: {step.Event} [{step.Line}:] !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_L2_Class(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
class Test2:                # LINE 2
    def __init__(self):     # LINE 3
        pass                # LINE 4
def Test():                 # LINE 5
    t = Test2()             # LINE 6
Test()                      # LINE 7
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                    new ( 6, ExecEvent.Line, DebugAction.StepIn),
                        new ( 4, ExecEvent.Line, DebugAction.StepOut),
                    new ( 6, ExecEvent.Return, DebugAction.StepOut),
                new ( 7, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 7));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"Expected: {step.Event} [{step.Line}:] !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_NoStoppingOnGlobal(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
import sys
m = 42
def Foo():      # CALL 4
    global m    # not pausing here
    pass        # LINE 6
Foo()           # LINE 7
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                new ( 6, ExecEvent.Line, DebugAction.StepIn),
                new ( 7, ExecEvent.Return, DebugAction.StepIn),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 7));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepIn(LanguageSpec spec)
        {

            Code code = GetLanguage(spec).CreateCode(
            $@"
import sys
def Foo():                          # CALL 3
    m = 12                          # LINE 4
class Test:
    def __init__(self):             # CALL 6
        self.some_value = 12        # LINE 7

def func_call_test():               # CALL 9
    def nested_func_call_test():    # LINE 10
        d = Test()                  # LINE 11
        Foo()                       # LINE 12
    Foo()                           # LINE 13
    nested_func_call_test()         # LINE 14

func_call_test()                    # LINE 16
Foo()                               # LINE 17
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // func_call_test()
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (10, ExecEvent.Line, DebugAction.StepIn),
                new (13, ExecEvent.Line, DebugAction.StepIn),
                // Foo()
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new (13, ExecEvent.Return, DebugAction.StepIn),
                new (14, ExecEvent.Line, DebugAction.StepIn),
                // nested_func_call_test()
                new (11, ExecEvent.Line, DebugAction.StepIn),
                // Test.__init__()
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Return, DebugAction.StepIn),
                // Foo()
                new (12, ExecEvent.Line, DebugAction.StepIn),
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new (12, ExecEvent.Return, DebugAction.StepIn),
                new (14, ExecEvent.Return, DebugAction.StepIn),
                new (16, ExecEvent.Return, DebugAction.StepIn),
                // Foo()
                new (17, ExecEvent.Line, DebugAction.StepIn),
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new (17, ExecEvent.Return, DebugAction.StepIn),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 16));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepIn_EndingPass(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
import sys
def Foo():                          # CALL 3
    pass                            # LINE 4
class Test:
    def __init__(self):             # CALL 6
        self.some_value = 12        # LINE 7

def func_call_test():               # CALL 9
    def nested_func_call_test():    # LINE 10
        d = Test()                  # LINE 11
        Foo()                       # LINE 12
    Foo()                           # LINE 13
    nested_func_call_test()         # LINE 14

func_call_test()                    # LINE 16
Foo()                               # LINE 17
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // func_call_test()
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (10, ExecEvent.Line, DebugAction.StepIn),
                new (13, ExecEvent.Line, DebugAction.StepIn),
                // Foo()
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new (13, ExecEvent.Return, DebugAction.StepIn),
                new (14, ExecEvent.Line, DebugAction.StepIn),
                // nested_func_call_test()
                new (11, ExecEvent.Line, DebugAction.StepIn),
                // Test.__init__()
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Return, DebugAction.StepIn),
                // Foo()
                new (12, ExecEvent.Line, DebugAction.StepIn),
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new (12, ExecEvent.Return, DebugAction.StepIn),
                new (14, ExecEvent.Return, DebugAction.StepIn),
                new (16, ExecEvent.Return, DebugAction.StepIn),
                // Foo()
                new (17, ExecEvent.Line, DebugAction.StepIn),
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new (17, ExecEvent.Return, DebugAction.StepIn),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 16));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepIn_Recursive(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
def recursive(a):           # CALL 2
    if a == 0:              # LINE 3
        return              # LINE / RETURN 4
    recursive(a - 1)        # LINE 5
recursive(5)                # LINE 6
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 6, ExecEvent.Line, DebugAction.StepIn),

                // 5
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                // 4
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                // 3
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                // 2
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                // 1
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                // 0
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new ( 4, ExecEvent.Line, DebugAction.StepIn),

                // unroll
                new ( 5, ExecEvent.Return, DebugAction.StepIn), //1
                new ( 5, ExecEvent.Return, DebugAction.StepIn), //2
                new ( 5, ExecEvent.Return, DebugAction.StepIn), //3
                new ( 5, ExecEvent.Return, DebugAction.StepIn), //4
                new ( 5, ExecEvent.Return, DebugAction.StepIn), //5

                new ( 6, ExecEvent.Return, DebugAction.StepIn),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 6));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOut(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import sys
def Foo():                          # CALL 3
    m = 42                          # LINE 4
class Test:
    def __init__(self):             # CALL 6
        self.some_value = 12        # LINE 7

def func_call_test():               # CALL 9
    def nested_func_call_test():    # LINE 10
        d = Test()                  # LINE 11
        Foo()                       # LINE 12
    Foo()                           # LINE 13
    nested_func_call_test()         # LINE 14

func_call_test()                    # LINE 16
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (10, ExecEvent.Line, DebugAction.StepOver),
                new (13, ExecEvent.Line, DebugAction.StepOver),
                new (14, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepIn),

                // step out
                new ( 7, ExecEvent.Line, DebugAction.StepOut),
                new (11, ExecEvent.Return, DebugAction.StepOut),
                new (14, ExecEvent.Return, DebugAction.StepOut),
                new (16, ExecEvent.Return, DebugAction.StepOut),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 16));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOut_EndingPass(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import sys
def Foo():                          # CALL 3
    pass                            # LINE 4
class Test:
    def __init__(self):             # CALL 6
        self.some_value = 12        # LINE 7

def func_call_test():               # CALL 9
    def nested_func_call_test():    # LINE 10
        d = Test()                  # LINE 11
        Foo()                       # LINE 12
    Foo()                           # LINE 13
    nested_func_call_test()         # LINE 14

func_call_test()                    # LINE 16
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (10, ExecEvent.Line, DebugAction.StepOver),
                new (13, ExecEvent.Line, DebugAction.StepOver),
                new (14, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepIn),

                // step out
                new ( 7, ExecEvent.Line, DebugAction.StepOut),
                new (11, ExecEvent.Return, DebugAction.StepOut),
                new (14, ExecEvent.Return, DebugAction.StepOut),
                new (16, ExecEvent.Return, DebugAction.StepOut),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 16));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOver_Exception_Handled(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
def func_test_error_inner():                    # CALL 2
    try:                                        # LINE 3
        raise Exception('Last Line Error')      # LINE / EXCEPTION 4
    except:
        pass
def func_test_error():                          # CALL 7
    func_test_error_inner()                     # LINE 8
func_test_error()                               # LINE 9
func_test_error()                               # LINE 10
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 9, ExecEvent.Line, DebugAction.StepOver),
                new (10, ExecEvent.Line, DebugAction.StepOver),
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
            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOut_L1(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
import sys
class Test:
    def __init__(self):
        self.some_value = 12

Test()  # LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepOut),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 7));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOut_L2(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import sys
def Foo():                          # CALL 3
    pass                            # LINE 4
class Test:
    def __init__(self):             # CALL 6
        self.some_value = 12        # LINE 7

def func_call_test():               # CALL 9
    def nested_func_call_test():    # LINE 10
        d = Test()                  # LINE 11
        Foo()                       # LINE 12
    Foo()                           # LINE 13
    nested_func_call_test()         # LINE 14

func_call_test()                    # LINE 16
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // func_call_test()
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (10, ExecEvent.Line, DebugAction.StepOver),
                new (13, ExecEvent.Line, DebugAction.StepOut),
                new (16, ExecEvent.Return, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 16));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOut_L3(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import sys
def Foo():                          # CALL 3
    pass                            # LINE 4
class Test:
    def __init__(self):             # CALL 6
        self.some_value = 12        # LINE 7

def func_call_test():               # CALL 9
    def nested_func_call_test():    # LINE 10
        d = Test()                  # LINE 11
        Foo()                       # LINE 12
    Foo()                           # LINE 13
    nested_func_call_test()         # LINE 14

func_call_test()                    # LINE 16
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // func_call_test()
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (10, ExecEvent.Line, DebugAction.StepOver),
                new (13, ExecEvent.Line, DebugAction.StepOver),
                new (14, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepOut),
                new (14, ExecEvent.Return, DebugAction.StepOver),
                new (16, ExecEvent.Return, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 16));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOut_L3_LastLine(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import sys
def Foo():                          # CALL 3
    pass                            # LINE 4
class Test:
    def __init__(self):             # CALL 6
        self.some_value = 12        # LINE 7

def func_call_test():               # CALL 9
    def nested_func_call_test():    # LINE 10
        d = Test()                  # LINE 11
        Foo()                       # LINE 12
    Foo()                           # LINE 13
    nested_func_call_test()         # LINE 14

func_call_test()                    # LINE 16
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // func_call_test()
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (10, ExecEvent.Line, DebugAction.StepOver),
                new (13, ExecEvent.Line, DebugAction.StepOver),
                new (14, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepOver),
                new (12, ExecEvent.Line, DebugAction.StepOut),
                new (14, ExecEvent.Return, DebugAction.StepOver),
                new (16, ExecEvent.Return, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 16));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOver_L1(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import sys
def func_call_test():
    pass

func_call_test()                    # LINE 6
func_call_test()                    # LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 6));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOver_ForLoop(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
import sys
def Foo():
    pass
for _ in range(2):      # LINE 5
    Foo()               # LINE 6
Foo()                   # LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // before
                new ( 5, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 5, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 5, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 5));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepOver_WhileLoop(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
m = 2
while(m > 0):   # LINE 3
    m -= 1      # LINE 4
print(m)        # LINE 5
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // before
                new (3, ExecEvent.Line, DebugAction.StepOver),

                // 2
                new ( 4, ExecEvent.Line, DebugAction.StepOver),
                new ( 3, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 4, ExecEvent.Line, DebugAction.StepOver),
                new ( 3, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 3));
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

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_Continue_Exception_Handled(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
def func_test_error_inner():                    # CALL 2
    try:                                        # LINE 3
        raise Exception('Last Line Error')      # LINE / EXCEPTION 4
    except:
        pass
def func_test_error():                          # CALL 7
    func_test_error_inner()                     # LINE 8
func_test_error()                               # LINE 9
func_test_error()                               # LINE 10
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 9, ExecEvent.Line, DebugAction.Continue),
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
            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_Continue_Exception_Handled_PauseOnAny(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
def func_test_error_inner():                    # CALL 2
    try:                                        # LINE 3
        raise Exception('Last Line Error')      # LINE / EXCEPTION 4
    except:
        pass
def func_test_error():                          # CALL 7
    func_test_error_inner()                     # LINE 8
func_test_error()                               # LINE 9
func_test_error()                               # LINE 10
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 9, ExecEvent.Line, DebugAction.Continue),
                new ( 4, ExecEvent.Exception, DebugAction.Continue),
            };
            controls.SetPauseOnExceptionPolicy(DebugPauseOnExceptionPolicy.PauseOnAny);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 9));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"{step.Line} !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;
            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        static IEnumerable<TestCaseData> GetPythonsAndDebugActions()
        {
            yield return new(LanguageSpec.Python2, DebugAction.Continue);
            yield return new(LanguageSpec.Python2, DebugAction.StepIn);
            yield return new(LanguageSpec.Python2, DebugAction.StepOut);
            yield return new(LanguageSpec.Python2, DebugAction.StepOver);

            yield return new(LanguageSpec.Python3, DebugAction.Continue);
            yield return new(LanguageSpec.Python3, DebugAction.StepIn);
            yield return new(LanguageSpec.Python3, DebugAction.StepOut);
            yield return new(LanguageSpec.Python3, DebugAction.StepOver);
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_StepIn_Exception_Handled(LanguageSpec spec)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
def func_test_error_inner():                    # CALL 2
    try:                                        # LINE 3
        raise Exception('Last Line Error')      # LINE 4
    except:                                     # LINE 5
        pass                                    # LINE 6
def func_test_error():                          # CALL 7
    func_test_error_inner()                     # LINE 8
func_test_error()                               # LINE 9
func_test_error()                               # LINE 10
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 9, ExecEvent.Line, DebugAction.StepIn),
                new ( 8, ExecEvent.Line, DebugAction.StepIn),
                new ( 3, ExecEvent.Line, DebugAction.StepOver),
                new ( 4, ExecEvent.Line, DebugAction.StepOver),
                new ( 5, ExecEvent.Line, DebugAction.Stop),
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
            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythonsAndDebugActions))]
        public void TestPython_DebugTracing_StepIn_Exception_Handled_PauseOnAny(LanguageSpec spec, DebugAction action)
        {
            Code code = GetLanguage(spec).CreateCode(
            $@"
def func_test_error_inner():                    # CALL 2
    try:                                        # LINE 3
        raise Exception('Last Line Error')      # LINE / EXCEPTION 4
    except:
        pass
def func_test_error():                          # CALL 7
    func_test_error_inner()                     # LINE 8
func_test_error()                               # LINE 9
func_test_error()                               # LINE 10
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 9, ExecEvent.Line, DebugAction.StepIn),
                new ( 8, ExecEvent.Line, DebugAction.StepIn),
                new ( 3, ExecEvent.Line, DebugAction.StepOver),
                new ( 4, ExecEvent.Line, DebugAction.StepOver),
                new ( 4, ExecEvent.Exception, action),
            };
            controls.SetPauseOnExceptionPolicy(DebugPauseOnExceptionPolicy.PauseOnAny);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 9));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"{step.Line} !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;
            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythonsAndDebugActions))]
        public void TestPython_DebugTracing_Exception_StepIn(LanguageSpec spec, DebugAction action)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
def func_test_error():
    raise Exception('Last Line Error')
func_test_error()
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new ( 3, ExecEvent.Line, DebugAction.StepOver),
                new ( 3, ExecEvent.Exception, action),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 4));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"{step.Line} !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;

            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythonsAndDebugActions))]
        public void TestPython_DebugTracing_Exception_StepOver(LanguageSpec spec, DebugAction action)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
def func_test_error():
    raise Exception('Last Line Error')
func_test_error()
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 4, ExecEvent.Line, DebugAction.StepOver),
                new ( 3, ExecEvent.Exception, action),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 4));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"{step.Line} !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;

            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythonsAndDebugActions))]
        public void TestPython_DebugTracing_Exception_ForLoop(LanguageSpec spec, DebugAction action)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
total = 0
for i in range(0, 3):
    total += i
    raise ExecuteException(""EX"")
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 3, ExecEvent.Line, DebugAction.StepOver),
                new ( 4, ExecEvent.Line, DebugAction.StepOver),
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
                new ( 5, ExecEvent.Exception, action),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 3));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"{step.Line} !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;

            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythonsAndDebugActions))]
        public void TestPython_DebugTracing_Exception_StepIn_DoNotPauseOnException(LanguageSpec spec, DebugAction action)
        {
            Code code = GetLanguage(spec).CreateCode(
$@"
def func_test_error():
    raise Exception('Last Line Error')
func_test_error()
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new ( 3, ExecEvent.Line, action),
            };
            controls.SetPauseOnExceptionPolicy(DebugPauseOnExceptionPolicy.PauseOnNone);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 4));
            controls.PauseOnStep += (ExpectedPauseEventStep step, ExecFrame frame) =>
            {
                bool pass = frame.Event == step.Event && frame.Reference.Position.LineNumber == step.Line;
                if (!pass)
                    TestContext.Progress.WriteLine($"{step.Line} !! {frame.Event} {frame.Reference.Position}");
                Assert.IsTrue(pass);
            };

            code.DebugControls = controls;

            try
            {
                code.Debug(new DebugContext());
            }
            catch (ExecuteException ex)
            {
                if (ex.InnerException is TestException te)
                    throw te;
            }
        }

        [Test, TestCaseSource(nameof(GetPythons))]
        public void TestPython_DebugTracing_Pass_Single(LanguageSpec spec)
        {

            Code code = GetLanguage(spec).CreateCode(
            $@"
import sys
def Foo():      # CALL 3
    pass        # LINE 4
Foo()           # LINE 5
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                new ( 4, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Return, DebugAction.StepIn),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 5));
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
    }
}
