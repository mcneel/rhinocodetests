using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using NUnit.Framework;
using NUnit.Framework.Internal;

using Rhino;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Testing;
using Rhino.Runtime.Code.Text;
using System.Text.RegularExpressions;

#if RC8_11
using RhinoCodePlatform.Rhino3D.Languages.GH1;
#else
using RhinoCodePlatform.Rhino3D.Languages;
#endif

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class CSharp_Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestCSharp_Script(ScriptInfo scriptInfo)
        {
            TestSkip(scriptInfo);

            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(scriptInfo.Uri);

            RunContext ctx = GetRunContext(scriptInfo);

            ctx.AutoApplyParams = true;
            ctx.OutputStream = GetOutputStream();
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
        public void TestCSharp_CompileErrorLine_MissingFunction()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
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
#if RC8_11
                if (ex.Diagnosis.First().Reference.Position.LineNumber != 4)
#else
                if (ex.Diagnostics.First().Reference.Position.LineNumber != 4)
#endif
                    throw;
            }
        }

        [Test]
        public void TestCSharp_Compile_Script()
        {
            // assert throws compile exception on run/debug/profile
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using Rhino;

a = x + y;
b = new Sphere(Point3d.Origin, x);
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
        public void TestCSharp_RuntimeErrorLine_InScript()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
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
        public void TestCSharp_RuntimeErrorLine_InFunction()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
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

        [Test]
        public void TestCSharp_Runtime_NULL_Input()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
a = x is null;
");

            RunContext ctx = GetRunContext();
            ctx.Inputs.Set("x", null);
            ctx.Outputs.Set("a", false);

            code.Run(ctx);

            bool isnull = ctx.Outputs.Get<bool>("a");
            Assert.IsTrue(isnull);
        }

        [Test]
        public void TestCSharp_DebugStop()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
Console.WriteLine(); // line 3
");

            // create debug controls to capture exception on line 9
            // note that a breakpoint on the line must be added. the controls
            // will stepOver the line to capture the exception event.
            var breakpoint = new CodeReferenceBreakpoint(code, 3);
            var controls = new DebugStopperControls(breakpoint);

            var ctx = new DebugContext();

            code.DebugControls = controls;


            Assert.Throws<DebugStopException>(() => code.Debug(ctx));
        }

        [Test]
        public void TestCSharp_DebugPauses_Script()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using Rhino;
using Rhino.Geometry;

Console.WriteLine(""RunScript"");
a = x + y; // line 7

var sphere = new Sphere(Point3d.Origin, x);
b = (int)sphere.Radius;
");

            var breakpoint = new CodeReferenceBreakpoint(code, 7);
            var controls = new DebugPauseDetectControls(breakpoint);

            using var swallow = new SwallowOutputsStream();
            code.DebugControls = controls;
            code.Debug(new DebugContext
            {
                AutoApplyParams = true,
                OutputStream = swallow,
                Inputs =
                {
                    ["x"] = 21,
                    ["y"] = 21,
                },
                Outputs =
                {
                    ["a"] = 0,
                    ["b"] = 0,
                }
            });

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugPauses_ScriptInstance()
        {
            const string INSTANCE = "__instance__";

            // detect missing variables in global scope does not break debugging.
            // this could happen if roslyn trace-injector does not produce valid code
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
$@"
using System;
using Rhino;

public class Script_Instance
{{
  public void RunScript(double x, double y, ref object a)
  {{
    a = x + y; // line 9
  }}
}}

{INSTANCE} = new Script_Instance();
");

            using DebugContext instctx = new() { AutoApplyParams = true, Outputs = { [INSTANCE] = default } };
            code.Run(instctx);
            dynamic instance = instctx.Outputs.Get(INSTANCE);

            var breakpoint = new CodeReferenceBreakpoint(code, 9);
            var controls = new DebugPauseDetectControls(breakpoint);
            code.DebugControls = controls;

            using (DebugContext ctx = new())
            {
#if RC8_11
                using DebugGroup g = code.DebugWith(ctx);
#else
                using DebugGroup g = code.DebugWith(ctx, invokes: true);
#endif
                object a = default;
                instance.RunScript(21, 21, ref a);
            }

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugVars_Script()
        {
            // detect auto-declare code params are in global scope.
            // this could happen if roslyn trace-injector does not produce valid code
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using Rhino;

a = x + y; // line 5
");

            var breakpoint = new CodeReferenceBreakpoint(code, 5);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("x", 21),
                new("y", 21),
            });

            code.DebugControls = controls;
            var ctx = new DebugContext
            {
                AutoApplyParams = true,
                Inputs =
                {
                    ["x"] = 21,
                    ["y"] = 21,
                },
                Outputs =
                {
                    ["a"] = 0,
                }
            };
            code.Debug(ctx);

            Assert.True(controls.Pass);
            Assert.AreEqual(42, ctx.Outputs.Get<int>("a"));
        }

        [Test]
        public void TestCSharp_DebugVars_ScriptInstance()
        {
            // detect missing variables in global scope does not break debugging.
            // this could happen if roslyn trace-injector does not produce valid code
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using Rhino;

Console.WriteLine(""Test""); // line 5

public class Script_Instance
{
  public void RunScript(double x, double y, ref object a)
  {
    a = x + y;
  }
}
");

            code.Inputs.Add(new Param("x") { AutoDeclare = false });
            code.Inputs.Add(new Param("y") { AutoDeclare = false });

            var breakpoint = new CodeReferenceBreakpoint(code, 5);
            var controls = new DebugVerifyEmptyVarsControls(breakpoint);

            using var swallow = new SwallowOutputsStream();
            code.DebugControls = controls;
            code.Debug(new DebugContext() { OutputStream = swallow });

            Assert.True(controls.Pass);
        }

#if RC8_9
        [Test]
        public void TestCSharp_DebugPauses_Script_StepOver()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
void First()
{
    Pass(); // line 6
    Pass(); // line 7
}

First();
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepOver);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 7));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugPauses_Script_StepOut()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
void First()
{
    Pass(); // line 6
    Pass(); // line 7
}

First();
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepOut);
            controls.DoNotExpectPause(new CodeReferenceBreakpoint(code, 7));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugPauses_Script_DoNotStepIn()
        {
            // detect auto-declare code params are in global scope.
            // this could happen if roslyn trace-injector does not produce valid code
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
void First()
{
    Pass(); // line 6
    Pass();
}

First();
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepIn);
            controls.DoNotExpectPause(new CodeReferenceBreakpoint(code, 3));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugReturn_Script()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
int Second()
{
    Pass(); // line 6
    return 12; // line 7
}
void First()
{
    Second();
}

First();
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.StepOver);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 7));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugNested_Script()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
void Second() {}
void First()
{
    Pass(); // line 7
    Second(); // line 8
    Pass();
}

First();
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 7), DebugAction.StepOver);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 8), DebugAction.Continue);

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugNestedNested_Script()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
int Second()
{
    Pass(); // line 6
    Pass();
    Pass();
    return 12;
}
void First()
{
    Pass();
    Second(); // line 14
    Pass();
}

First();
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 14), DebugAction.StepIn);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.Continue);

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert()
        {
            var script = new Grasshopper1Script(@"
// #! csharp
// Grasshopper Script
using System;
a = ""Hello Python 3 in Grasshopper!"";
Console.WriteLine(a);
");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            // FIXME:
            // comments are removed in C# conversion
            Assert.AreEqual(@"
// #! csharp
// Grasshopper Script
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
  /* 
    Members:
      RhinoDoc RhinoDocument
      GH_Document GrasshopperDocument
      IGH_Component Component
      int Iteration

    Methods (Virtual & overridable):
      Print(string text)
      Print(string format, params object[] args)
      Reflect(object obj)
      Reflect(object obj, string method_name)
  */

  private void RunScript()
  {
    a = ""Hello Python 3 in Grasshopper!"";
    Console.WriteLine(a);
  }
}

", script.Text);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert_LastEmptyLine()
        {
            var script = new Grasshopper1Script(@"
// #! csharp
// Grasshopper Script
using System;
a = ""Hello Python 3 in Grasshopper!"";
Console.WriteLine(a);");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"
// #! csharp
// Grasshopper Script
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
  /* 
    Members:
      RhinoDoc RhinoDocument
      GH_Document GrasshopperDocument
      IGH_Component Component
      int Iteration

    Methods (Virtual & overridable):
      Print(string text)
      Print(string format, params object[] args)
      Reflect(object obj)
      Reflect(object obj, string method_name)
  */

  private void RunScript()
  {
    a = ""Hello Python 3 in Grasshopper!"";
    Console.WriteLine(a);  }
}

", script.Text);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert_WithFunction()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82125
            var script = new Grasshopper1Script(@"
// #! csharp
// Grasshopper Script
using System;
a = ""Hello Python 3 in Grasshopper!"";
Console.WriteLine(a);

int Test()
{
    return 42;
}
");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"
// #! csharp
// Grasshopper Script
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
  /* 
    Members:
      RhinoDoc RhinoDocument
      GH_Document GrasshopperDocument
      IGH_Component Component
      int Iteration

    Methods (Virtual & overridable):
      Print(string text)
      Print(string format, params object[] args)
      Reflect(object obj)
      Reflect(object obj, string method_name)
  */

  private void RunScript()
  {
    a = ""Hello Python 3 in Grasshopper!"";
    Console.WriteLine(a);
  }

  private int Test()
  {
    return 42;
  }
}

", script.Text);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert_WithFunctionWithParams()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82125
            var script = new Grasshopper1Script(@"
// #! csharp
// Grasshopper Script
using System;
a = ""Hello Python 3 in Grasshopper!"";
Console.WriteLine(a);

int Test(int x, int y)
{
    return 42;
}
");

            script.ConvertToScriptInstance(addSolve: false, addPreview: false);

            // NOTE:
            // no params are defined so RunScript() is empty
            Assert.AreEqual(@"
// #! csharp
// Grasshopper Script
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
  /* 
    Members:
      RhinoDoc RhinoDocument
      GH_Document GrasshopperDocument
      IGH_Component Component
      int Iteration

    Methods (Virtual & overridable):
      Print(string text)
      Print(string format, params object[] args)
      Reflect(object obj)
      Reflect(object obj, string method_name)
  */

  private void RunScript()
  {
    a = ""Hello Python 3 in Grasshopper!"";
    Console.WriteLine(a);
  }

  private int Test(int x, int y)
  {
    return 42;
  }
}

", script.Text);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert_AddSolveOverrides()
        {
            var script = new Grasshopper1Script(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }
}
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: false);

            Assert.AreEqual(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }

    public override void BeforeRunScript()
    {
    }

    public override void AfterRunScript()
    {
    }
}
", script.Text);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert_AddPreviewOverrides()
        {
            var script = new Grasshopper1Script(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }
}
");

            script.ConvertToScriptInstance(addSolve: false, addPreview: true);

            Assert.AreEqual(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }

    public override BoundingBox ClippingBox => BoundingBox.Empty;

    public override void DrawViewportMeshes(IGH_PreviewArgs args)
    {
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
    }
}
", script.Text);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert_AddBothOverrides()
        {
            var script = new Grasshopper1Script(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }
}
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: true);

            Assert.AreEqual(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }

    public override void BeforeRunScript()
    {
    }

    public override void AfterRunScript()
    {
    }

    public override BoundingBox ClippingBox => BoundingBox.Empty;

    public override void DrawViewportMeshes(IGH_PreviewArgs args)
    {
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
    }
}
", script.Text);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Convert_AddBothOverrides_Steps()
        {
            var script = new Grasshopper1Script(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }
}
");

            script.ConvertToScriptInstance(addSolve: true, addPreview: false);
            script.ConvertToScriptInstance(addSolve: false, addPreview: true);

            Assert.AreEqual(@"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }

    public override void BeforeRunScript()
    {
    }

    public override void AfterRunScript()
    {
    }

    public override BoundingBox ClippingBox => BoundingBox.Empty;

    public override void DrawViewportMeshes(IGH_PreviewArgs args)
    {
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
    }
}
", script.Text);
        }
#endif

#if RC8_11
        [Test]
        public void TestCSharp_Library()
        {
            ILanguage csharp = GetLanguage(LanguageSpec.CSharp);

            TryGetTestFilesPath(out string fileDir);
            ILanguageLibrary library = csharp.CreateLibrary(new Uri(Path.Combine(fileDir, "cs", "test_library")));

            Assert.True(library.GetCodes().All(c => LanguageSpec.CSharp.Matches(c.LanguageSpec)));
        }

        [Test]
        public void TestCSharp_DebugDisconnects()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83214
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Test(int v) { value = v; return; }
void First()
{
    Test(0);  // line 6
    Test(42); // line 7
}

First();
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 6), DebugAction.Disconnect);
            controls.DoNotExpectPause(new CodeReferenceBreakpoint(code, 7));

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
        public void TestCSharp_DebugTracer_VoidLambda()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83216
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
int value = 0;

void Test(int v) => value = v;

Test(42);
");

            code.DebugControls = new DebugContinueAllControls();
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        [Test]
        public void TestCSharp_TextFlagLookup()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
$@"
// flag: grasshopper.inputs.marshaller.asStructs
using System;
");

            var ctx = new RunContext();
            code.Run(ctx);

            Assert.IsTrue(ctx.Options.Get("grasshopper.inputs.marshaller.asStructs", false));
        }
#endif

#if RC8_12
        [Test]
        public void TestCSharp_AwaitPass()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// async: true
using System;
using System.Threading;
using System.Threading.Tasks;
 
async Task<int> Compute()
{
    await Task.Delay(TimeSpan.FromMilliseconds(2000));
    return 42;
}
 
int result = await Compute();
");

            Assert.DoesNotThrow(() => code.Build(new BuildContext()));
        }

        [Test]
        public void TestCSharp_AwaitPass_IfStatement()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// async: true
using System;
using System.Threading;
using System.Threading.Tasks;
 
async Task<bool> Compute()
{
    await Task.Delay(TimeSpan.FromMilliseconds(2000));
    return true;
}
 
if (await Compute()) { }
");

            Assert.DoesNotThrow(() => code.Build(new BuildContext()));
        }

        [Test]
        public void TestCSharp_AwaitFail()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using System.Threading;
using System.Threading.Tasks;
 
async Task<int> Compute()
{
    await Task.Delay(TimeSpan.FromMilliseconds(2000));
    return 42;
}
 
int result = await Compute();
");

            CompileException ex = Assert.Throws<CompileException>(() => code.Build(new BuildContext()));

            Assert.AreEqual(1, ex.Diagnosis.Length);

            Diagnostic d = ex.Diagnosis.First();
            Assert.AreEqual(DiagnosticSeverity.Error, d.Severity);
            Assert.AreEqual("The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.", d.Message);
        }

        [Test]
        public void TestCSharp_AwaitFail_IfStatement()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using System.Threading;
using System.Threading.Tasks;
 
async Task<bool> Compute()
{
    await Task.Delay(TimeSpan.FromMilliseconds(2000));
    return true;
}
 
if (await Compute()) { }
");

            CompileException ex = Assert.Throws<CompileException>(() => code.Build(new BuildContext()));

            Assert.AreEqual(1, ex.Diagnosis.Length);

            Diagnostic d = ex.Diagnosis.First();
            Assert.AreEqual(DiagnosticSeverity.Error, d.Severity);
            Assert.AreEqual("The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.", d.Message);
        }

        [Test]
        public void TestCSharp_Threaded_ExclusiveStreams()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode("using System;Console.WriteLine($\"{a} {b}\");");

            string[] outputs = RunManyExclusiveStreams(code, 3);

            Assert.AreEqual($"21 21{Environment.NewLine}", outputs[0]);
            Assert.AreEqual($"22 22{Environment.NewLine}", outputs[1]);
            Assert.AreEqual($"23 23{Environment.NewLine}", outputs[2]);
        }

        [Test]
        public void TestCSharp_Threaded_ExclusiveStreams_NestedFunction()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(@"
using System;

void testConsole()
{
    Console.WriteLine($""{a} {b}"");
}

testConsole();
");

            string[] outputs = RunManyExclusiveStreams(code, 3);

            Assert.AreEqual($"21 21{Environment.NewLine}", outputs[0]);
            Assert.AreEqual($"22 22{Environment.NewLine}", outputs[1]);
            Assert.AreEqual($"23 23{Environment.NewLine}", outputs[2]);
        }

        [Test]
        public void TestCSharp_Threaded_ExclusiveStreams_StaticFunction()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(@"
using System;

Test.TestConsole(a, b);

static class Test {
public static void TestConsole(int a, int b)
{
    Console.WriteLine($""{a} {b}"");
}}
");

            string[] outputs = RunManyExclusiveStreams(code, 3);

            Assert.AreEqual(string.Empty, outputs[0]);
            Assert.AreEqual(string.Empty, outputs[1]);
            Assert.AreEqual(string.Empty, outputs[2]);
        }
#endif

#if RC8_13
        [Test]
        public void TestCSharp_DebugReturn_FromCtor()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
class F {
    public F()
    {
        Pass();
    }
    public void Pass() { }
    public void Show() { }
}
var f = new F();    // line 11
f.Show();           // line 12
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 11), DebugAction.StepOver);
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 12));

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugIf_Scope()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
if (int.TryParse(""12"", out int res))
    Pass();             // line 5
");

            var breakpoint = new CodeReferenceBreakpoint(code, 5);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("test", 12),
            });
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugIfElse_Scope()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
object v = ""t"";
if (v is int test)
    Pass();
// trace calls in else clause should not 'res'
else
    Pass();
");

            var controls = new DebugPauseDetectControls();
            code.DebugControls = controls;

            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        [Test]
        public void TestCSharp_DebugIfElse_Scope_Out_If()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
if (int.TryParse(""12"", out int res))
    Pass();             // line 5
// trace calls in else clause should report 'res'
else
    Pass();              // line 8
");

            var breakpoint = new CodeReferenceBreakpoint(code, 5);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("res", 12),
            });
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugIfElse_Scope_Out_Else()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
if (int.TryParse(""-"", out int res))
    Pass();             // line 5
// trace calls in else clause should report 'res'
else
    Pass();              // line 8
");

            var breakpoint = new CodeReferenceBreakpoint(code, 8);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("res", 0),
            });
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugIfElse_Scope_Is_If()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
object v = 12;
if (v is int test)
    Pass();     // line 6
// trace calls in else clause should not 'res'
else
    Pass();
");

            var breakpoint = new CodeReferenceBreakpoint(code, 6);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("test", 12),
            });
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugIfElse_Scope_Is_Else()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void Pass() {}
object v = ""-"";
if (v is int test)
    Pass();
// trace calls in else clause should not 'res'
else
    Pass();     // line 9
");

            var breakpoint = new CodeReferenceBreakpoint(code, 9);
            var controls = new DebugVerifyVarsControls(breakpoint, new UnexpectedVariable[]
            {
                new("test"),
            });
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugSwitch_Scope()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83950
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
int test = 12;
switch (test)
{
    case 10:
        int m = 42;
        Console.WriteLine(m);
        break;
    case 12:
        Console.WriteLine(""'m' should not be here"");
        break;
}
");

            var controls = new DebugPauseDetectControls();
            code.DebugControls = controls;

            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        [Test]
        public void TestCSharp_DebugSwitch_Scope_Expected()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83950
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
int test = 10;
switch (test)
{
    case 10:
        int m = 42;
        Console.WriteLine(m); // line 8
        break;
    case 12:
        Console.WriteLine(""'m' should not be here"");
        break;
}
");

            var breakpoint = new CodeReferenceBreakpoint(code, 8);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("m", 42),
            });
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_DebugSwitch_Scope_NotExpected()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83950
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
int test = 12;
switch (test)
{
    case 10:
        int m = 42;
        Console.WriteLine(m);
        break;
    case 12:
        Console.WriteLine(""'m' should not be here""); // line 11
        break;
}
");

            var breakpoint = new CodeReferenceBreakpoint(code, 11);
            var controls = new DebugVerifyVarsControls(breakpoint, new UnexpectedVariable[]
            {
                new("m"),
            });
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }
#endif

#if RC8_15
        [Test]
        public void TestCSharp_DebugThis_ClassMethod()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81598
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
class F {
    public void Test() {
        Console.WriteLine(); // line 5
    }
}
var f = new F();
f.Test();
");

            var breakpoint = new CodeReferenceBreakpoint(code, 5);
            var controls = new DebugVerifyVarsControls(breakpoint, new ExpectedVariable[]
            {
                new("this"),
            });

            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_ExecSpec_Platform_First()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// platform: rhino3d@8
// platform: revit 2023
using System;
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetPlatformSpec(out PlatformSpec pspec));
            Assert.AreEqual(new PlatformSpec("*.*.rhino3d", "8.*.*"), pspec);
        }

        [Test]
        public void TestCSharp_ExecSpec_Platform_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// platform: 
// platform: rhino3d@8
using System;
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetPlatformSpec(out PlatformSpec pspec));
            Assert.AreEqual(new PlatformSpec("*.*.rhino3d", "8.*.*"), pspec);
        }

        [Test]
        public void TestCSharp_ExecSpec_Async_First()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// async: true
// async: false
using System;
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
        public void TestCSharp_ExecSpec_Async_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// async: maybe
// async: true
using System;
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
        public void TestCSharp_ExecSpec_EnvironId_First()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// venv: first_custom
// venv: second_custom
using System;
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetEnvironId(out string environid));
            Assert.AreEqual("first_custom", environid);
        }

        [Test]
        public void TestCSharp_ExecSpec_EnvironId_FirstValid()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// venv: 
// venv: first_custom
using System;
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetEnvironId(out string environid));
            Assert.AreEqual("first_custom", environid);
        }

        [Test]
        public void TestCSharp_Flags_Defaults()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode();

            ReadOnlyContextOptions cdefautls = code.GetContextOptionsDefaults();
            foreach (string key in cdefautls)
            {
                Assert.False(cdefautls.Get<bool>(key));
            }
        }

        [Test]
        public void TestCSharp_Flags()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
# flag: csharp.compiler.optimize
# flag: csharp.compiler.unsafe
import os
");

            ExecSpecifierResult execSpec = code.Text.GetExecSpecs();

            Assert.True(execSpec.TryGetContextOptions(out ReadOnlyContextOptions opts));
            Assert.True(opts.Get("csharp.compiler.optimize", false));
            Assert.True(opts.Get("csharp.compiler.unsafe", false));
        }

#if RC8_19
        // class SortTextComparer : IComparer<CompletionInfo>
        // {
        //     public int Compare(CompletionInfo x, CompletionInfo y)
        //     {
        //         string xs = x.SortText;
        //         string ys = y.SortText;

        //         if (char.IsUpper(xs[0]) && char.IsLower(ys[0]))
        //             return -1; // Uppercase first
        //         if (char.IsLower(xs[0]) && char.IsUpper(ys[0]))
        //             return 1;  // Lowercase later

        //         return string.Compare(xs, ys, StringComparison.Ordinal);  // Regular comparison
        //     }
        // }

        static IEnumerable<CompletionInfo> CompleteAtPosition(Code code, int position, CompleteOptions? options = default)
        {
            code.Language.Support.BeginSupport(code);
            IEnumerable<CompletionInfo> completions = code.Language.Support.Complete(SupportRequest.Empty, code, position, options ?? CompleteOptions.Empty);
            code.Language.Support.EndSupport(code);

            // NOTE:
            // using sortText to order completions to match Monaco behaviour
            return completions.OrderBy(c => c.SortText);
        }

        static IEnumerable<SignatureInfo> CompleteSignatureAtPosition(Code code, int position, CompleteSignatureOptions? options = default)
        {
            code.Language.Support.BeginSupport(code);
            IEnumerable<SignatureInfo> completions = code.Language.Support.CompleteSignature(SupportRequest.Empty, code, position, options ?? CompleteSignatureOptions.Empty);
            code.Language.Support.EndSupport(code);
            return completions;
        }
#else
        static IEnumerable<Ed.Common.CompletionItem> CompleteAtEndingPeriod(Code code, string textUptoPeriod)
        {
            if (code.Text.TryGetPosition(textUptoPeriod.Length, out TextPosition position))
            {
                if (!code.Text.TryGetTransformed(textUptoPeriod.Length, CompleteOptions.Empty, out string xformedCode, out int xformedPosition))
                {
                    xformedCode = code.Text;
                    xformedPosition = textUptoPeriod.Length;
                }

                return s_dispatcher.InvokeAsync(() =>
                {
                    CSharpCompletionProvider.CompletionProvider provider = GetCompletionProvider(code);
                    return provider.GetCompletionItems(xformedCode, xformedPosition, position.LineNumber, position.ColumnNumber, '.');
                }).GetAwaiter().GetResult();
            }

            return Array.Empty<Ed.Common.CompletionItem>();
        }
#endif

        static CSharpCompletionProvider.CompletionProvider GetCompletionProvider(Code code)
        {
            CSharpCompletionProvider.CompletionProvider provider =
                CSharpCompletionProvider.CompletionProvider.Create(
                    CSharpCompletionProvider.CompletionProvider.DefaultUsings,
                    CSharpCompletionProvider.CompletionProvider.DefaultAssemblies.Select(r => r.Location).ToList()
                );

            AddCSharpReferences(provider, code.Language.Runtime.References);
            AddCSharpReferences(provider, RhinoCode.Platforms.GetReferences());
            AddCSharpReferences(provider, code.References);

            if (code.Text.GetPackageSpecs()
                    .Packages.TryResolveReferences(code, out IEnumerable<CompileReference> references, out Diagnosis _))
            {
                AddCSharpReferences(provider, references);
            }

            return provider;
        }

        static void AddCSharpReferences(CSharpCompletionProvider.CompletionProvider provider, IEnumerable<CompileReference> references)
        {
            foreach (string path in references.GetAssemblies().Select(r => r.Path))
            {
                provider.AddAssembly(path);
            }
        }

        [Test]
        public void TestCSharp_Compile_Unsafe_Error()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81598
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;

unsafe
{
    int d = 42;
}
");

            ExecuteException run = Assert.Throws<ExecuteException>(() => code.Run(new RunContext()));
            Assert.IsInstanceOf(typeof(CompileException), run.InnerException);
        }

        [Test]
        public void TestCSharp_Compile_Unsafe()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-81598
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
// flag: csharp.compiler.unsafe
using System;

unsafe
{
    int d = 42;
}
");

            Assert.DoesNotThrow(() => code.Run(new RunContext()));
        }

        [Test]
        public void TestCSharp_Complete_Provider()
        {
            string s = "using System.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains(nameof(System.Reflection), names);
            Assert.Contains(nameof(System.Collections), names);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Complete_Self()
        {
            string s = @"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
        this.";
            var script = new Grasshopper1Script(s + @"
    }
}
");

            Code code = script.CreateCode();

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains("Component", names);
            Assert.Contains("GrasshopperDocument", names);
            Assert.Contains("Iteration", names);
            Assert.Contains("RhinoDocument", names);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Complete_SelfRhinoDoc()
        {
            string s = @"// #! csharp
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
        this.RhinoDocument.";
            var script = new Grasshopper1Script(s + @"
    }
}
");

            Code code = script.CreateCode();

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains("ActiveCommandId", names);
            Assert.Contains("Objects", names);
        }

        [Test]
        public void TestCSharp_ScriptInstance_Complete_AssemblyReference()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-72523
            string s = @"// #! csharp
# r ""{0}""
using System;
using Rhino;
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
        zTools.";
            s = s.Replace("{0}", GetTestScript("cs", "zTools.dll").First());
            var script = new Grasshopper1Script(s + @"
    }
}
");

            Code code = script.CreateCode();

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains("CurveFeature", names);
            Assert.Contains("MeshFeature", names);
        }

        [Test]
        public void TestCSharp_CompileGuard_Library()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84921
            ILanguage csharp = GetLanguage(LanguageSpec.CSharp);

            TryGetTestFilesPath(out string fileDir);
            ILanguageLibrary library = csharp.CreateLibrary(new Uri(Path.Combine(fileDir, "cs", "test_library_compileguard")));

            Assert.IsTrue(library.TryBuild(new LibraryBuildOptions(), out CompileReference cred, out Diagnosis _));

            byte[] data = File.ReadAllBytes(cred.Path);
            Assembly a = Assembly.Load(data);

            Type t = a.GetType("Test_Library_CompileGuard.Test");
            dynamic inst = Activator.CreateInstance(t);

            Assert.AreEqual(42, inst.TestLibrary());
        }

        [Test]
        public void TestCSharp_CompileGuard_Library_Not()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84921
            ILanguage csharp = GetLanguage(LanguageSpec.CSharp);

            TryGetTestFilesPath(out string fileDir);
            ILanguageLibrary library = csharp.CreateLibrary(new Uri(Path.Combine(fileDir, "cs", "test_library_compileguard_not")));

            // NOTE:
            // using BuildOptions does not add LIBRARY compile guard
            var opts = new LibraryBuildOptions();
            opts.CompileGuards.Remove(LibraryBuildOptions.DEFINE_LIBRARY.Identifier);
            Assert.IsTrue(library.TryBuild(opts, out CompileReference cred, out Diagnosis _));

            byte[] data = File.ReadAllBytes(cred.Path);
            Assembly a = Assembly.Load(data);

            Type t = a.GetType("Test_Library_CompileGuard_Not.Test");
            dynamic inst = Activator.CreateInstance(t);

            Assert.AreEqual(42, inst.TestNotLibrary());
        }

        [Test]
        public void TestCSharp_Complete_ProjectServerArgs_RhinoCommand()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85037
            string s = @"// #! csharp
using System;
Console.WriteLine(__rhino_command__.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @");
Console.WriteLine(__rhino_doc__);
Console.WriteLine(__rhino_runmode__);
Console.WriteLine(__is_interactive__);
");

            code.Inputs.Set(RhinoCode.ProjectServers.GetArguments(LanguageSpec.CSharp));

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains(nameof(Rhino.Commands.Command.Id), names);
            Assert.Contains(nameof(Rhino.Commands.Command.EnglishName), names);
            Assert.Contains(nameof(Rhino.Commands.Command.LocalName), names);
        }

        [Test]
        public void TestCSharp_Complete_ProjectServerArgs_RhinoDoc()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85037
            string s = @"// #! csharp
using System;
Console.WriteLine(__rhino_command__);
Console.WriteLine(__rhino_doc__.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @");
Console.WriteLine(__rhino_runmode__);
Console.WriteLine(__is_interactive__);
");

            code.Inputs.Set(RhinoCode.ProjectServers.GetArguments(LanguageSpec.CSharp));

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains(nameof(Rhino.RhinoDoc.Bitmaps), names);
            Assert.Contains(nameof(Rhino.RhinoDoc.HatchPatterns), names);
        }

        [Test]
        public void TestCSharp_Complete_ProjectServerArgs_RhinoRunMode()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85037
            string s = @"// #! csharp
using System;
Console.WriteLine(__rhino_command__);
Console.WriteLine(__rhino_doc__);
Console.WriteLine(__rhino_runmode__.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @");
Console.WriteLine(__is_interactive__);
");

            code.Inputs.Set(RhinoCode.ProjectServers.GetArguments(LanguageSpec.CSharp));

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();

            Assert.IsNotEmpty(completions);

            Assert.Contains(nameof(Enum.HasFlag), names);
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();

            Assert.IsNotEmpty(completions);

            Assert.Contains("byte", names);
            Assert.Contains("char", names);
            Assert.Contains(nameof(Enum.HasFlag), names);
#endif

        }

        [Test]
        public void TestCSharp_Complete_ProjectServerArgs_RhinoRunModeIsInteractive()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85037
            string s = @"// #! csharp
using System;
Console.WriteLine(__rhino_command__);
Console.WriteLine(__rhino_doc__);
Console.WriteLine(__rhino_runmode__);
Console.WriteLine(__is_interactive__.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @");
");

            code.Inputs.Set(RhinoCode.ProjectServers.GetArguments(LanguageSpec.CSharp));

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);
            Assert.Contains(nameof(bool.TryFormat), names);
        }

        [Test]
        public void TestCSharp_Complete_ProjectServerArgs_PositionInUsings()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85098
            string s = @"// #! csharp
using System;
using System.Linq;
using System.Threading;
using System.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @"
using Rhino;

var lcid = Rhino.ApplicationSettings.AppearanceSettings.LanguageIdentifier;
var culture = System.Globalization.CultureInfo.GetCultureInfo(lcid);

Console.WriteLine(lcid);

Console.WriteLine(culture);
Console.WriteLine(Thread.CurrentThread.CurrentCulture);
Console.WriteLine(Thread.CurrentThread.CurrentUICulture);
");

            code.Inputs.Set(RhinoCode.ProjectServers.GetArguments(LanguageSpec.CSharp));

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains(nameof(System.Threading), names);
            Assert.Contains(nameof(System.Globalization), names);
        }

        [Test]
        public void TestCSharp_CompileGuard_Library_AsCode()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84921
            ILanguage csharp = GetLanguage(LanguageSpec.CSharp);

            // ensure main.cs is executed including types.cs and
            // without LIBRARY compile guard
            TryGetTestFilesPath(out string fileDir);
            Uri mainFile = new(Path.Combine(fileDir, "cs", "test_library_as_code", "Main.cs"));
            Uri otherFile = new(Path.Combine(fileDir, "cs", "test_library_as_code", "Types.cs"));

            Code code = csharp.CreateCode(mainFile);
            code.Text.References.Add(new SourceCode(otherFile));

            int _add_;
            double _solve_;

            var ctx = new RunContext
            {
                AutoApplyParams = true,
                Outputs =
                {
                    [nameof(_add_)] = 0,
                    [nameof(_solve_)] = 0d,
                }
            };

            Assert.DoesNotThrow(() => code.Run(ctx));

            Assert.IsTrue(ctx.Outputs.TryGet(nameof(_add_), out _add_));
            Assert.AreEqual(52, _add_);

            Assert.IsTrue(ctx.Outputs.TryGet(nameof(_solve_), out _solve_));
            Assert.AreEqual(42, _solve_);
        }

        [Test]
        public void TestCSharp_CompileGuard_Library_AsCode_Debug()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84921
            ILanguage csharp = GetLanguage(LanguageSpec.CSharp);

            // ensure main.cs is executed including types.cs and
            // without LIBRARY compile guard,
            // and containing DEBUG guard
            TryGetTestFilesPath(out string fileDir);
            Uri mainFile = new(Path.Combine(fileDir, "cs", "test_library_as_code", "Main.cs"));
            Uri otherFile = new(Path.Combine(fileDir, "cs", "test_library_as_code", "Types.cs"));

            Code code = csharp.CreateCode(mainFile);
            code.Text.References.Add(new SourceCode(otherFile));

            int _add_;
            double _solve_;

            var ctx = new DebugContext
            {
                AutoApplyParams = true,
                Outputs =
                {
                    [nameof(_add_)] = 0,
                    [nameof(_solve_)] = 0d,
                }
            };

            code.DebugControls = new DebugContinueAllControls();
            Assert.DoesNotThrow(() => code.Debug(ctx));

            Assert.IsTrue(ctx.Outputs.TryGet(nameof(_add_), out _add_));
            Assert.AreEqual(500, _add_);

            Assert.IsTrue(ctx.Outputs.TryGet(nameof(_solve_), out _solve_));
            Assert.AreEqual(42, _solve_);
        }
#endif

#if RC8_16
        [Test]
        public void TestCSharp_DebugCompile_ScriptInstance()
        {
            var script = new Grasshopper1Script(@"// #! csharp
// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion

    private void RunScript(object x, object y, ref object a)
    {
        // Write your logic here
        a = null;
    }
}
");

            Code code = script.CreateCode();
            code.DebugControls = new DebugContinueAllControls();
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
        }

        static string GetScriptClassSource(Code code)
        {
            code.Text.TryGetTransformed(new RunContext(), out string scriptClassSource);
            return scriptClassSource;
        }

        static IEnumerable<TestCaseData> GetScriptClassSourceCases()
        {
            yield return new(@"
#region Usings
using System;
#endregion

__context__.Outputs[""__gh_scriptinstance__""] = new Script_Instance();class Script_Instance : RhinoCodePlatform.Rhino3D.Languages.GH1.Grasshopper1ScriptInstance
{
    private void RunScript(object x, object y, ref object a)
    {
        a = default;
    }
}
",
@"
#region Usings
using System;
#endregion

sealed class __RhinoCodeScript__{[Rhino.Runtime.Code.Execution.SourceFrameAttribute(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){
__context__.Outputs[""__gh_scriptinstance__""] = new Script_Instance();
}
}
class Script_Instance : RhinoCodePlatform.Rhino3D.Languages.GH1.Grasshopper1ScriptInstance
{
    private void RunScript(object x, object y, ref object a)
    {
        a = default;
    }
}

")
            { TestName = "TestCSharp_ScriptClassSource_RegionBeforeStatement" };
        }

        [Test, TestCaseSource(nameof(GetScriptClassSourceCases))]
        public void TestCSharp_ScriptClassSource(string source, string expected)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(source);

            string scriptClassSource = GetScriptClassSource(code);
            // TestContext.Progress.WriteLine(scriptClassSource.Replace("\"", "\"\""));
            Assert.AreEqual(expected, scriptClassSource);
        }

        [Test]
        public void TestCSharp_CompileGuard_Specific()
        {
            int major = RhinoApp.Version.Major;
            int minor = RhinoApp.Version.Minor;

            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode($@"
using System;

result = false;
#if RHINO_{major}_{minor}
result = true;
#endif
");

            RunContext ctx = GetRunContext(captureStdout: false);

            ctx.AutoApplyParams = true;
            ctx.Outputs["result"] = default;

            Assert.DoesNotThrow(() => code.Run(ctx));
            Assert.True(ctx.Outputs.TryGet("result", out bool data));
            Assert.True(data);
        }

        [Test]
        public void TestCSharp_Compile_LastInlineComment()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85148
            Code code = GetLanguage(LanguageSpec.CSharp9).CreateCode(@"
using System;
Console.WriteLine(); // Comment");

            Assert.DoesNotThrow(() => code.Build(new BuildContext()));
        }

        [Test]
        public void TestCSharp_Complete_ScriptInstance_XformerStack()
        {
            // this is a test to make sure C# autocompletion transformer stack
            // is processing in correct order
            string s = @"// #! csharp
using System;
using Rhino.";
            Code code = new Grasshopper1Script(s + @"
using Grasshopper;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object x, object y, ref object a)
    {
    }
}
").CreateCode();

#if RC8_19
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();
#else
            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);
            string[] names = completions.Select(c => c.label).ToArray();
#endif

            Assert.IsNotEmpty(completions);

            Assert.Contains(nameof(Rhino.Geometry), names);
            Assert.Contains(nameof(Rhino.Display), names);
            Assert.Contains(nameof(Rhino.Runtime), names);
            Assert.Contains(nameof(Rhino.UI), names);
        }

        [Test]
        public void TestCSharp_ContextTracking()
        {
            const int THREAD_COUNT = 5;
            const string CID_NAME = "__cid__";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode($@"
using System;
{CID_NAME} = __context__.Id.Id;
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

        [Test]
        public void TestCSharp_ContextTracking_CurrentContext()
        {
            const int THREAD_COUNT = 5;
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(@"
// async: true
using System.Threading.Tasks;
await Task.Delay(1000);
");

            using RunGroup group = code.RunWith("test");
            int counter = 0;
            Parallel.For(0, THREAD_COUNT, (i) =>
            {
                var ctx = new RunContext($"Thread {i}");
                _ = code.RunAsync(ctx);

                Assert.AreEqual(ctx.Id, code.ContextTracker.CurrentContext);
                Interlocked.Increment(ref counter);
            });

            Assert.AreEqual(THREAD_COUNT, counter);
        }

        [Test]
        public void TestCSharp_ContextTracking_MainContext()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

var ts = new List<Task>();
ts.Add(Task.Run(T.Work));
ts.Add(Task.Run(T.Work));
Task.WaitAll(ts.ToArray());

static class T
{
    public static void Work()
    {
        Pass(); // line 17
    }
    public static void Pass() {}
}
");

            int counter = 0;
            var dctx = new DebugContext();
            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 17));
            controls.Paused += (c) =>
            {
                ContextIdentity context = c.Results.CurrentContext;
                Assert.AreEqual(dctx.Id, context);
                counter++;
            };

            code.DebugControls = controls;
            code.Debug(dctx);

            Assert.True(controls.Pass);
            Assert.AreEqual(2, counter);
        }

        [Test]
        public void TestCSharp_ContextTracking_GroupContext_Many()
        {
            const int THREAD_COUNT = 5;
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

var ts = new List<Task>();
ts.Add(Task.Run(T.Work));
ts.Add(Task.Run(T.Work));
Task.WaitAll(ts.ToArray());

static class T
{
    public static void Work()
    {
        Pass(); // line 17
    }
    public static void Pass() {}
}
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 17));

            code.DebugControls = controls;

            using DebugGroup group = code.DebugWith("test");
            int counter = 0;
            Parallel.For(0, THREAD_COUNT, (i) =>
            {
                var dctx = new DebugContext($"Thread {i}");
                controls.Paused += (c) =>
                {
                    ContextIdentity context = c.Results.CurrentContext;
                    Assert.AreEqual(group.Context.Id, context);
                };

                code.Debug(dctx);
                Interlocked.Increment(ref counter);
            });

            Assert.AreEqual(THREAD_COUNT, counter);
            Assert.True(controls.Pass);
        }

        [Test]
        public void TestCSharp_ContextTracking_FirstContext_Many()
        {
            //NOTE:
            // see notes on Code.ContextTracker.CurrentContext
            const int THREAD_COUNT = 25;
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

var ts = new List<Task>();
ts.Add(Task.Run(T.Work));
ts.Add(Task.Run(T.Work));
Task.WaitAll(ts.ToArray());

static class T
{
    public static void Work()
    {
        Pass(); // line 17
    }
    public static void Pass() {}
}
");

            var controls = new DebugPauseDetectControls();
            controls.ExpectPause(new CodeReferenceBreakpoint(code, 17));

            code.DebugControls = controls;

            int counter = 0;
            var contexts = new ConcurrentBag<ContextIdentity>();
            Parallel.For(0, THREAD_COUNT, (i) =>
            {
                var dctx = new DebugContext($"Thread {i}");
                contexts.Add(dctx.Id);

                controls.Paused += (c) =>
                {
                    ContextIdentity context = c.Results.CurrentContext;
                    Assert.Contains(context, contexts);
                };

                code.Debug(dctx);
                Interlocked.Increment(ref counter);
            });

            Assert.AreEqual(THREAD_COUNT, counter);
            Assert.True(controls.Pass);
        }

        static IEnumerable<TestCaseData> GetL2Sources()
        {
            yield return new($@"
void Test() {{     // LINE 2
    int m = 42;    // LINE 3
}}
Test();            // LINE 5
", new StackAction[]
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 5),
                // entering level 2
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 3),
                new (StackActionKind.Swapped, ExecEvent.Line, 3, ExecEvent.Return, 3),
                // returning to level 1
                new (StackActionKind.Swapped, ExecEvent.Line, 5, ExecEvent.Return, 5)
            })
            { TestName = "TestCSharp_DebugTracing_StackWatch_Function_L2_CompactBrace" };

            yield return new($@"
void Test()        // LINE 2
{{
    int m = 42;    // LINE 4
}}
Test();            // LINE 6
", new StackAction[]
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 6),
                // entering level 2
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 4),
                new (StackActionKind.Swapped, ExecEvent.Line, 4, ExecEvent.Return, 4),
                // returning to level 1
                new (StackActionKind.Swapped, ExecEvent.Line, 6, ExecEvent.Return, 6)
            })
            { TestName = "TestCSharp_DebugTracing_StackWatch_Function_L2_ExpandedBrace" };

            yield return new($@"
void Test()        // LINE 2
{{ int m = 42;     // LINE 3
}}
Test();            // LINE 6
", new StackAction[]
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 5),
                // entering level 2
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 3),
                new (StackActionKind.Swapped, ExecEvent.Line, 3, ExecEvent.Return, 3),
                // returning to level 1
                new (StackActionKind.Swapped, ExecEvent.Line, 5, ExecEvent.Return, 5)
            })
            { TestName = "TestCSharp_DebugTracing_StackWatch_Function_L2_ExpandedBraceSameLine" };
        }

        [Test, TestCaseSource(nameof(GetL2Sources))]
        public void TestCSharp_DebugTracing_StackWatch_Function_L2(string source, StackAction[] actions)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(source);
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual);
            foreach (StackAction action in actions)
                controls.Add(action);

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        [Test]
        public void TestCSharp_DebugTracing_StackWatch_Function_L3()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
            $@"
void Test2() {{    // LINE 2
    int m = 42;    // LINE 3
}}
void Test() {{     // LINE 5
    Test2();       // LINE 6
}}
Test();            // LINE 8
");
            var controls = new DebugStackActionsWatcher(TestContext.Progress.WriteLine, Assert.AreEqual)
            {
                // start
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 2),
                new (StackActionKind.Swapped, ExecEvent.Line, 2, ExecEvent.Line, 5),
                new (StackActionKind.Swapped, ExecEvent.Line, 5, ExecEvent.Line, 8),
                // entering level 2
                new (StackActionKind.Pushed, ExecEvent.Call, 5, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 5, ExecEvent.Line, 6),
                // entering level 3
                new (StackActionKind.Pushed, ExecEvent.Call, 2, 0, 0),
                new (StackActionKind.Swapped, ExecEvent.Call, 2, ExecEvent.Line, 3),
                new (StackActionKind.Swapped, ExecEvent.Line, 3, ExecEvent.Return, 3),
                // returning to level 2
                new (StackActionKind.Swapped, ExecEvent.Line, 6, ExecEvent.Return, 6),
                // returning to level 1
                new (StackActionKind.Swapped, ExecEvent.Line, 8, ExecEvent.Return, 8),
            };

            code.DebugControls = controls;
            Assert.DoesNotThrow(() => code.Debug(new DebugContext()));
            Assert.AreEqual(0, controls.Count);
        }

        static IEnumerable<TestCaseData> GetLoopVariableSources()
        {
            const string INDEX_VAR = "i";
            const string SUM_VAR = "sum";

            yield return new($@"
using System;
using System.Linq;

int {SUM_VAR} = 0;
for (int {INDEX_VAR} = 0; i < 3; i++) // line 6
{{
    {SUM_VAR} += {INDEX_VAR};   // line 8
}}
", INDEX_VAR, SUM_VAR) { TestName = "TestCSharp_DebugTracing_LoopVariable_ForLoop" };

            yield return new($@"
using System;
using System.Linq;

int {SUM_VAR} = 0;
foreach (int {INDEX_VAR} in Enumerable.Range(0, 3)) // line 6
{{
    {SUM_VAR} += {INDEX_VAR};   // line 8
}}
", INDEX_VAR, SUM_VAR) { TestName = "TestCSharp_DebugTracing_LoopVariable_ForEachLoop" };
        }

        [Test, TestCaseSource(nameof(GetLoopVariableSources))]
        public void TestCSharp_DebugTracing_LoopVariable(string source, string index, string sum)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85276
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(source);

            DebugExpressionExecVariableResult getIndex(ExecFrame frame)
            {
                return frame.Evaluate().OfType<DebugExpressionExecVariableResult>().FirstOrDefault(r => r.Result.Id.Identifier == index);
            }

            DebugExpressionExecVariableResult getSum(ExecFrame frame)
            {
                return frame.Evaluate().OfType<DebugExpressionExecVariableResult>().FirstOrDefault(r => r.Result.Id.Identifier == sum);
            }

            int bp6Counter = -1;
            int bp8Counter = 0;
            var bp6 = new CodeReferenceBreakpoint(code, 6);
            var bp8 = new CodeReferenceBreakpoint(code, 8);

            ExecFrame prevFrame = ExecFrame.Empty;
            var controls = new DebugContinueAllControls();
            controls.Breakpoints.Add(bp6);
            controls.Breakpoints.Add(bp8);
            controls.Paused += (IDebugControls c) =>
            {
                if (c.Results.CurrentThread.CurrentFrame is ExecFrame frame
                        && ExecEvent.Line == frame.Event)
                {
                    if (bp6.Matches(frame))
                    {
                        switch (bp6Counter)
                        {
                            // first arrive at line 6
                            // i does not exist
                            case -1:
                                DebugExpressionExecVariableResult er = getIndex(frame);
                                Assert.IsNull(er);
                                break;

                            // i = 0
                            case 0:
                                DebugExpressionExecVariableResult er0 = getIndex(frame);
                                Assert.IsInstanceOf<DebugExpressionExecVariableResult>(er0);
                                // i does not exist in previous frame
                                Assert.IsFalse(frame.HasModifiedResult(er0, prevFrame));
                                Assert.IsTrue(er0.Result.TryGetValue(out int v0));
                                Assert.AreEqual(0, v0);
                                break;

                            // i = 1
                            case 1:
                                DebugExpressionExecVariableResult er1 = getIndex(frame);
                                Assert.IsFalse(frame.HasModifiedResult(er1, prevFrame));
                                Assert.IsTrue(er1.Result.TryGetValue(out int v1));
                                Assert.AreEqual(1, v1);

                                DebugExpressionExecVariableResult ers1 = getSum(frame);
                                Assert.IsTrue(frame.HasModifiedResult(ers1, prevFrame));      // sum is modified!
                                Assert.IsTrue(ers1.Result.TryGetValue(out int sum1));
                                Assert.AreEqual(1, sum1);
                                break;

                            // i = 2
                            case 2:
                                DebugExpressionExecVariableResult er2 = getIndex(frame);
                                Assert.IsFalse(frame.HasModifiedResult(er2, prevFrame));
                                Assert.IsTrue(er2.Result.TryGetValue(out int v2));
                                Assert.AreEqual(2, v2);

                                DebugExpressionExecVariableResult ers2 = getSum(frame);
                                Assert.IsTrue(frame.HasModifiedResult(ers2, prevFrame));      // sum is modified!
                                Assert.IsTrue(ers2.Result.TryGetValue(out int sum2));
                                Assert.AreEqual(3, sum2);
                                break;

                            // breakpoint 6 will never see i as 2
                            default:
                                Assert.Fail("breakpoint 6 should never see i as 2");
                                break;
                        }
                        bp6Counter++;
                    }

                    else
                    if (bp8.Matches(frame))
                    {
                        switch (bp8Counter)
                        {
                            // first arrive at line 8
                            // i = 0
                            case 0:
                                DebugExpressionExecVariableResult er0 = getIndex(frame);
                                // csharp does not stop twice on for loop so
                                // i is not available in frame previous to this
                                Assert.IsFalse(frame.HasModifiedResult(er0, prevFrame));
                                Assert.IsTrue(er0.Result.TryGetValue(out int v0));
                                Assert.AreEqual(0, v0);
                                break;

                            // i = 1
                            case 1:
                                DebugExpressionExecVariableResult er1 = getIndex(frame);
                                Assert.IsTrue(frame.HasModifiedResult(er1, prevFrame));      // i is modified!
                                Assert.IsTrue(er1.Result.TryGetValue(out int v1));
                                Assert.AreEqual(1, v1);

                                DebugExpressionExecVariableResult ers0 = getSum(frame);
                                Assert.IsInstanceOf<DebugExpressionExecVariableResult>(ers0);
                                Assert.IsFalse(frame.HasModifiedResult(ers0, prevFrame));
                                Assert.IsTrue(ers0.Result.TryGetValue(out int sum0));
                                Assert.AreEqual(0, sum0);
                                break;

                            // i = 2
                            case 2:
                                DebugExpressionExecVariableResult er2 = getIndex(frame);
                                Assert.IsTrue(frame.HasModifiedResult(er2, prevFrame));      // i is modified!
                                Assert.IsTrue(er2.Result.TryGetValue(out int v2));
                                Assert.AreEqual(2, v2);

                                DebugExpressionExecVariableResult ers1 = getSum(frame);
                                // sum is not modified since it was 1 on entering loop on previous pause
                                Assert.IsFalse(frame.HasModifiedResult(ers1, prevFrame));
                                Assert.IsTrue(ers1.Result.TryGetValue(out int sum1));
                                Assert.AreEqual(1, sum1);
                                break;

                            // breakpoint 8 will never see sum as 2
                            default:
                                Assert.Fail("breakpoint 8 should never see sum as 2");
                                break;
                        }
                        bp8Counter++;
                    }

                    prevFrame = frame;
                }
            };
            code.DebugControls = controls;
            code.Debug(new DebugContext());

            Assert.AreEqual(-1 + 4, bp6Counter);
            Assert.AreEqual(3, bp8Counter);
        }

        [Test]
        public void TestCSharp_DebugTracing_StackWatch_L1_Single()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
int m = 42;
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

        [Test]
        public void TestCSharp_DebugTracing_L1()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Test() {      // LINE 2
    int m = 42;    // LINE 3
}
Test();            // LINE 5
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                    new ( 3, ExecEvent.Line, DebugAction.StepOut),
                new ( 5, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 5));
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

        [Test]
        public void TestCSharp_DebugTracing_L1_DoBlock()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Test() {      // LINE 2
    int m = 42;    // LINE 3
}
Test();            // LINE 5
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 5, ExecEvent.Line, DebugAction.StepIn),
                    new ( 3, ExecEvent.Line, DebugAction.StepOut),
                new ( 5, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 5));
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

        [Test]
        public void TestCSharp_DebugTracing_L1_WithStructuredTrivia()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
            @"
using System;

#if DEBUG

int a = 42;         // LINE 6

#else
int a = 0;          // LINE 9
#endif
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (6, ExecEvent.Line, DebugAction.StepOver),
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

        [Test]
        public void TestCSharp_DebugTracing_L1_Lambda_Return()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"using System;

Func<int> c1 = () =>
{
    return 42;                // LINE 5
};

c1();                         // LINE 8
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 8, ExecEvent.Line, DebugAction.StepIn),
                    new ( 5, ExecEvent.Line, DebugAction.StepIn),
                new ( 8, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 8));
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

        [Test]
        public void TestCSharp_DebugTracing_L2()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Test2() {
    int a = 42;     // LINE 3
}
void Test() {
    Test2();        // LINE 6
}
Test();             // LINE 8
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 8, ExecEvent.Line, DebugAction.StepIn),
                    new ( 6, ExecEvent.Line, DebugAction.StepIn),
                        new ( 3, ExecEvent.Line, DebugAction.StepOut),
                    new ( 6, ExecEvent.Return, DebugAction.StepOut),
                new ( 8, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 8));
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

        [Test]
        public void TestCSharp_DebugTracing_L2_Class()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
class Test2 {
    public Test2() {
        int m = 42;         // LINE 4
    }
}
void Test() {
    var t = new Test2();    // LINE 8
}
Test();                     // LINE 10
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 10, ExecEvent.Line, DebugAction.StepIn),
                    new ( 8, ExecEvent.Line, DebugAction.StepIn),
                        new ( 4, ExecEvent.Line, DebugAction.StepOut),
                    new ( 8, ExecEvent.Return, DebugAction.StepOut),
                new ( 10, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 10));
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

        [Test]
        public void TestCSharp_DebugTracing_L2_WithStructuredTrivia()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
            @"
using System;

int Add(int x, int y)
{
#if DEBUG


    return 500;         // LINE 9


#else
    return x + y + 10;
#endif
}

Add(21, 21);            // LINE 17
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (17, ExecEvent.Line, DebugAction.StepIn),
                    new ( 9, ExecEvent.Line, DebugAction.StepOut),
                new (17, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 17));
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

        [Test]
        public void TestCSharp_DebugTracing_L2_Lambda_Return()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"using System;

Func<int> c2 = () => {
    return 42;                // LINE 4
};

Func<int> c1 = () =>
{
    return c2();              // LINE 9
};

c1();                         // LINE 12
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (12, ExecEvent.Line, DebugAction.StepIn),
                    new ( 9, ExecEvent.Line, DebugAction.StepIn),
                        new ( 4, ExecEvent.Line, DebugAction.StepIn),
                    new ( 9, ExecEvent.Return, DebugAction.StepOut),
                new (12, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 12));
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

        [Test]
        public void TestCSharp_DebugTracing_L3_Return()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
double ComputeThat(double x) {
    return x + 21;                  // LINE 3
}
double ComputeThis(double x)
{
    return ComputeThat(x);          // LINE 7
}
void Compute()
{
    ComputeThis(21);                // LINE 11
}
Compute();                          // LINE 13
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 13, ExecEvent.Line, DebugAction.StepIn),
                    new ( 11, ExecEvent.Line, DebugAction.StepIn),
                        new ( 7, ExecEvent.Line, DebugAction.StepIn),
                            new ( 3, ExecEvent.Line, DebugAction.StepOver),
                        new ( 7, ExecEvent.Return, DebugAction.StepOut),
                    new ( 11, ExecEvent.Return, DebugAction.StepOut),
                new ( 13, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 13));
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

        [Test]
        public void TestCSharp_DebugTracing_L3_Return_Middle()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
double ComputeThat(double x) {
    return x + 21;                  // LINE 3
}
double ComputeThis(double x)
{
    if (x == 42)
        return ComputeThat(x);      // LINE 8
    
    return 42;
}
void Compute()
{
    ComputeThis(42);                // LINE 14

    return;
}
Compute();                          // LINE 18
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 18, ExecEvent.Line, DebugAction.StepIn),
                    new ( 14, ExecEvent.Line, DebugAction.StepIn),
                        new ( 7, ExecEvent.Line, DebugAction.StepOver),
                        new ( 8, ExecEvent.Line, DebugAction.StepIn),
                            new ( 3, ExecEvent.Line, DebugAction.StepOver),
                        new ( 8, ExecEvent.Return, DebugAction.StepOut),
                    new ( 14, ExecEvent.Return, DebugAction.StepOut),
                new ( 18, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 18));
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

        [Test]
        public void TestCSharp_DebugTracing_L3_Return_End()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
double ComputeThat(double x) {
    return x + 21;                  // LINE 3
}
double ComputeThis(double x)
{
    if (x == 42)
        return ComputeThat(x);      // LINE 8
    
    return 42;
}
void Compute()
{
    ComputeThis(21);                // LINE 14

    return;
}
Compute();                          // LINE 18
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 18, ExecEvent.Line, DebugAction.StepIn),
                    new ( 14, ExecEvent.Line, DebugAction.StepIn),
                        new ( 7, ExecEvent.Line, DebugAction.StepOver),
                        new ( 10, ExecEvent.Line, DebugAction.StepIn),
                    new ( 14, ExecEvent.Return, DebugAction.StepOut),
                new ( 18, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 18));
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

        [Test]
        public void TestCSharp_DebugTracing_L3_Lambda_Return()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"using System;

Func<int> c3 = () => {
    return 42;                // LINE 4
};

Func<int> c2 = () =>
{
    return c3();              // LINE 9
};

Func<int> c1 = () =>
{
    return c2();              // LINE 14
};

c1();                         // LINE 17
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (17, ExecEvent.Line, DebugAction.StepIn),
                    new (14, ExecEvent.Line, DebugAction.StepIn),
                        new ( 9, ExecEvent.Line, DebugAction.StepIn),
                            new ( 4, ExecEvent.Line, DebugAction.StepIn),
                        new ( 9, ExecEvent.Return, DebugAction.StepOut),
                    new (14, ExecEvent.Return, DebugAction.StepOut),
                new (17, ExecEvent.Return, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 17));
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

        [Test]
        public void TestCSharp_DebugTracing_StepIn()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Foo() {
    var m = 12;                         // LINE 3
}
class Test {
    public Test() {
        var some_value = 12;            // LINE 7
    }
}
void func_call_test() {
    void nested_func_call_test() {      // LINE 11
        var d = new Test();             // LINE 12
        Foo();                          // LINE 13
    }
    Foo();                              // LINE 15
    nested_func_call_test();            // LINE 16
}
func_call_test();                       // LINE 18
Foo();                                  // LINE 19
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // func_call_test()
                new (18, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepIn),
                new (15, ExecEvent.Line, DebugAction.StepIn),
                // Foo()
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new (15, ExecEvent.Return, DebugAction.StepIn),
                new (16, ExecEvent.Line, DebugAction.StepIn),
                // nested_func_call_test()
                new (12, ExecEvent.Line, DebugAction.StepIn),
                // Test.__init__()
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                new (12, ExecEvent.Return, DebugAction.StepIn),
                // Foo()
                new (13, ExecEvent.Line, DebugAction.StepIn),
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new (13, ExecEvent.Return, DebugAction.StepIn),
                new (16, ExecEvent.Return, DebugAction.StepIn),
                new (18, ExecEvent.Return, DebugAction.StepIn),
                // Foo()
                new (19, ExecEvent.Line, DebugAction.StepIn),
                new ( 3, ExecEvent.Line, DebugAction.StepIn),
                new (19, ExecEvent.Return, DebugAction.StepIn),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 18));
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
        public void TestCSharp_DebugTracing_StepIn_Recursive()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void recursive(int a) {
    if (a == 0)             // LINE 3
        return;             // LINE 4
    recursive(a - 1);       // LINE 5
}
recursive(5);               // LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepIn),

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

        [Test]
        public void TestCSharp_DebugTracing_StepOut()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Foo() {
    var m = 12;                         // LINE 3
}
class Test {
    public Test() {
        var some_value = 12;            // LINE 7
    }
}
void func_call_test() {
    void nested_func_call_test() {      // LINE 11
        var d = new Test();             // LINE 12
        Foo();                          // LINE 13
    }
    Foo();                              // LINE 15
    nested_func_call_test();            // LINE 16
}
func_call_test();                       // LINE 18
Foo();                                  // LINE 19
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (18, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepOver),
                new (15, ExecEvent.Line, DebugAction.StepOver),
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (12, ExecEvent.Line, DebugAction.StepIn),

                // step out
                new ( 7, ExecEvent.Line, DebugAction.StepOut),
                new (12, ExecEvent.Return, DebugAction.StepOut),
                new (16, ExecEvent.Return, DebugAction.StepOut),
                new (18, ExecEvent.Return, DebugAction.StepOut),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 18));
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
        public void TestCSharp_DebugTracing_StepOver_Exception_Handled()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error_inner()
{
    try                                             // LINE 5
    {
        throw new Exception(""Handled Error"");     // LINE 7
    }
    catch {}
}
void func_test_error()
{
    func_test_error_inner();                        // LINE 13
}
func_test_error();                                  // LINE 15
func_test_error();                                  // LINE 16
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (15, ExecEvent.Line, DebugAction.StepOver),
                new (16, ExecEvent.Line, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 15));
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

        [Test]
        public void TestCSharp_DebugTracing_StepOut_L1()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
class Test {
    public Test() {
        var some_value = 12;    // LINE 4
    }
}

var d = new Test();             // LINE 8
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 8, ExecEvent.Line, DebugAction.StepOut),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 8));
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
        public void TestCSharp_DebugTracing_StepOut_L2()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Foo() {
    var m = 12;                         // LINE 3
}
class Test {
    public Test() {
        var some_value = 12;            // LINE 7
    }
}
void func_call_test() {
    void nested_func_call_test() {      // LINE 11
        var d = new Test();             // LINE 12
        Foo();                          // LINE 13
    }
    Foo();                              // LINE 15
    nested_func_call_test();            // LINE 16
}
func_call_test();                       // LINE 18
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // func_call_test()
                new (18, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepOver),
                new (15, ExecEvent.Line, DebugAction.StepOut),
                new (18, ExecEvent.Return, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 18));
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
        public void TestCSharp_DebugTracing_StepOut_L3()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Foo() {
    var m = 12;                         // LINE 3
}
class Test {
    public Test() {
        var some_value = 12;            // LINE 7
    }
}
void func_call_test() {
    void nested_func_call_test() {      // LINE 11
        var d = new Test();             // LINE 12
        Foo();                          // LINE 13
    }
    Foo();                              // LINE 15
    nested_func_call_test();            // LINE 16
}
func_call_test();                       // LINE 18
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (18, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepOver),
                new (15, ExecEvent.Line, DebugAction.StepOver),
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (12, ExecEvent.Line, DebugAction.StepOut),
                new (16, ExecEvent.Return, DebugAction.StepOver),
                new (18, ExecEvent.Return, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 18));
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
        public void TestCSharp_DebugTracing_StepOut_L3_LastLine()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void Foo() {
    var m = 12;                         // LINE 3
}
class Test {
    public Test() {
        var some_value = 12;            // LINE 7
    }
}
void func_call_test() {
    void nested_func_call_test() {      // LINE 11
        var d = new Test();             // LINE 12
        Foo();                          // LINE 13
    }
    Foo();                              // LINE 15
    nested_func_call_test();            // LINE 16
}
func_call_test();                       // LINE 18
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (18, ExecEvent.Line, DebugAction.StepIn),
                new (11, ExecEvent.Line, DebugAction.StepOver),
                new (15, ExecEvent.Line, DebugAction.StepOver),
                new (16, ExecEvent.Line, DebugAction.StepIn),
                new (12, ExecEvent.Line, DebugAction.StepOver),
                new (13, ExecEvent.Line, DebugAction.StepOut),
                new (16, ExecEvent.Return, DebugAction.StepOver),
                new (18, ExecEvent.Return, DebugAction.StepOver),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 18));
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
        public void TestCSharp_DebugTracing_StepOver_L1()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
void func_call_test() {
    var m = 12;                     // LINE 3
}

func_call_test();                   // LINE 6
func_call_test();                   // LINE 7
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

        static IEnumerable<TestCaseData> GetStepOverForLoopsCases()
        {
            yield return new(@"
void Foo() {
    var m = 12;              // LINE 3
}

for(int i =0; i < 2; i++) Foo();  // LINE 6

Foo();                       // LINE 8
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForLoop_NoBlock" };

            yield return new(@"
void Foo() {
    var m = 12;              // LINE 3
}

for(int i =0; i < 2; i++)    // LINE 6
    Foo();                   // LINE 7

Foo();                       // LINE 9
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 9, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForLoop_NoBlockExpanded" };

            yield return new(@"
void Foo() {
    var m = 12;              // LINE 3
}

for(int i =0; i < 2; i++) {  // LINE 6
    Foo();                   // LINE 7
}

Foo();                       // LINE 10
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new (10, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForLoop_CompactBrace" };

            yield return new(
@"
void Foo() {
    var m = 12;              // LINE 3
}

for(int i =0; i < 2; i++)    // LINE 6
{
    Foo();                   // LINE 8
}

Foo();                       // LINE 11
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new (11, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForLoop_ExpandedBrace" };

            yield return new(
@"
void Foo() {
    var m = 12;              // LINE 3
}

for(int i =0; i < 2; i++)    // LINE 6
{ Foo();                     // LINE 7
}

Foo();                       // LINE 10
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new (10, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForLoop_ExpandedBraceSameLine" };

            yield return new(
@"
void Foo() {
    var m = 12;              // LINE 3
}

for(int i =0; i < 2; i++)    // LINE 6
{
    int f = 42;              // LINE 8
    Foo();                   // LINE 9
}

Foo();                       // LINE 10
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 9, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 9, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new (12, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForLoop_WithVariableInLoopScope" };
        }

        [Test, TestCaseSource(nameof(GetStepOverForLoopsCases))]
        public void TestCSharp_DebugTracing_StepOver_ForLoop(string source, ExpectedPauseEventStep[] actions)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(source);

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>();
            foreach (ExpectedPauseEventStep step in actions)
                controls.Add(step);
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

        static IEnumerable<TestCaseData> GetStepOverForEachLoopsCases()
        {
            yield return new(@"
using System;
using System.Linq;

int total = 0;
foreach (int i in Enumerable.Range(0, 3)) total += i;   // line 6

int f = total;
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 2
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForEachLoop_NoBlock" };

            yield return new(@"
using System;
using System.Linq;

int total = 0;
foreach (int i in Enumerable.Range(0, 3))   // line 6
    total += i;   // line 7

int f = total;
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 2
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 9, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForEachLoop_NoBlockExpanded" };

            yield return new(@"
using System;
using System.Linq;

int total = 0;
foreach (int i in Enumerable.Range(0, 3)) { // LINE 6
    total += i;   // line 7
}
int f = total;
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 2
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 9, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForEachLoop_CompactBrace" };

            yield return new(
@"
using System;
using System.Linq;

int total = 0;
foreach (int i in Enumerable.Range(0, 3)) // LINE 6
{
    total += i;   // line 8
}
int f = total;
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 2
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new (10, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForEachLoop_ExpandedBrace" };

            yield return new(
@"
using System;
using System.Linq;

int total = 0;
foreach (int i in Enumerable.Range(0, 3)) // LINE 6
{ total += i;   // line 7
}
int f = total;
", new ExpectedPauseEventStep[]
            {
                // before
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 0
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // 2
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),

                // after
                new ( 9, ExecEvent.Line, DebugAction.StepOver),
            })
            { TestName = "TestCSharp_DebugTracing_StepOver_ForEachLoop_ExpandedBraceSameLine" };
        }

        [Test, TestCaseSource(nameof(GetStepOverForEachLoopsCases))]
        public void TestCSharp_DebugTracing_StepOver_ForEachLoop(string source, ExpectedPauseEventStep[] actions)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(source);

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>();
            foreach (ExpectedPauseEventStep step in actions)
                controls.Add(step);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 6));
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

        [Test]
        public void TestCSharp_DebugTracing_StepOver_WhileLoop()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
int m = 2;
while(m > 0) {
    m -= 1;
}
int f = m;
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                // before
                new (3, ExecEvent.Line, DebugAction.StepOver),
                new (3, ExecEvent.Line, DebugAction.StepOver),  // FIXME should not be here

                // 2
                new (4, ExecEvent.Line, DebugAction.StepOver),
                new (3, ExecEvent.Line, DebugAction.StepOver),

                // 1
                new (4, ExecEvent.Line, DebugAction.StepOver),
                // new ( 3, ExecEvent.Line, DebugAction.StepOver),  // FIXME shoud be here

                // after
                new (6, ExecEvent.Line, DebugAction.StepOver),
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

        [Test]
        public void TestCSharp_DebugTracing_Continue_Exception_Handled()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error_inner()
{
    try                                             // LINE 5
    {
        throw new Exception(""Handled Error"");     // LINE 7
    }
    catch {}
}
void func_test_error()
{
    func_test_error_inner();                        // LINE 13
}
func_test_error();                                  // LINE 15
func_test_error();                                  // LINE 16
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (15, ExecEvent.Line, DebugAction.Continue),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 15));
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

        [Test]
        public void TestCSharp_DebugTracing_Continue_Exception_Handled_PauseOnAny()
        {
            Assert.Ignore();
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error_inner()
{
    try                                             // LINE 5
    {
        throw new Exception(""Handled Error"");     // LINE 7
    }
    catch {}
}
void func_test_error()
{
    func_test_error_inner();                        // LINE 13
}
func_test_error();                                  // LINE 15
func_test_error();                                  // LINE 16
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (15, ExecEvent.Line, DebugAction.Continue),
                new ( 7, ExecEvent.Exception, DebugAction.Continue),
            };
            controls.SetPauseOnExceptionPolicy(DebugPauseOnExceptionPolicy.PauseOnAny);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 15));
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

        [Test]
        public void TestCSharp_DebugTracing_StepIn_Exception_Handled()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error_inner()
{
    try                                             // LINE 5
    {
        throw new Exception(""Handled Error"");     // LINE 7
    }
    catch {}
}
void func_test_error()
{
    func_test_error_inner();                        // LINE 13
}
func_test_error();                                  // LINE 15
func_test_error();                                  // LINE 16
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (15, ExecEvent.Line, DebugAction.StepIn),
                new (13, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new (13, ExecEvent.Return, DebugAction.Stop),
            };
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 15));
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

        static IEnumerable<TestCaseData> GetDebugActionCases()
        {
            yield return new(DebugAction.Continue);
            yield return new(DebugAction.StepIn);
            yield return new(DebugAction.StepOut);
            yield return new(DebugAction.StepOver);
        }

        [Test, TestCaseSource(nameof(GetDebugActionCases))]
        public void TestCSharp_DebugTracing_StepIn_Exception_Handled_PauseOnAny(DebugAction action)
        {
            Assert.Ignore();
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error_inner()
{
    try                                             // LINE 5
    {
        throw new Exception(""Handled Error"");     // LINE 7
    }
    catch {}
}
void func_test_error()
{
    func_test_error_inner();                        // LINE 13
}
func_test_error();                                  // LINE 15
func_test_error();                                  // LINE 16
");


            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new (15, ExecEvent.Line, DebugAction.StepIn),
                new (13, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Exception, action),
            };
            controls.SetPauseOnExceptionPolicy(DebugPauseOnExceptionPolicy.PauseOnAny);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 15));
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

        [Test, TestCaseSource(nameof(GetDebugActionCases))]
        public void TestCSharp_DebugTracing_Exception_StepIn(DebugAction action)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error()
{
    throw new Exception(""Handled Error"");     // LINE 5
}
func_test_error();                              // LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
                new ( 5, ExecEvent.Exception, action),
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

        [Test, TestCaseSource(nameof(GetDebugActionCases))]
        public void TestCSharp_DebugTracing_Exception_StepOver(DebugAction action)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error()
{
    throw new Exception(""Handled Error"");     // LINE 5
}
func_test_error();                              // LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Exception, action),
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

        static IEnumerable<TestCaseData> GetDebugTracingExceptionForLoopCases()
        {
            foreach (DebugAction action in GetDebugActionCases().Select(da => (DebugAction)da.Arguments[0]))
                yield return new(@"
using Rhino.Runtime.Code.Execution;

int total = 0;
for(int i =0; i < 3; i++) {                 // LINE 5
    total += i;                             // LINE 6
    throw new ExecuteException(""EX"");     // LINE 7
}
", new ExpectedPauseEventStep[]
                {
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
                new ( 6, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Exception, action),
                })
                { TestName = nameof(TestCSharp_DebugTracing_Exception_ForLoop) + $"_Compact_{action}" };

            foreach (DebugAction action in GetDebugActionCases().Select(da => (DebugAction)da.Arguments[0]))
                yield return new(@"
using Rhino.Runtime.Code.Execution;

int total = 0;
for(int i =0; i < 3; i++)                   // LINE 5
{
    total += i;                             // LINE 7
    throw new ExecuteException(""EX"");     // LINE 8
}
", new ExpectedPauseEventStep[]
                {
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
                new ( 7, ExecEvent.Line, DebugAction.StepOver),
                new ( 8, ExecEvent.Line, DebugAction.StepOver),
                new ( 8, ExecEvent.Exception, action),
                })
                { TestName = nameof(TestCSharp_DebugTracing_Exception_ForLoop) + $"_Expanded_{action}" };
        }

        [Test, TestCaseSource(nameof(GetDebugTracingExceptionForLoopCases))]
        public void TestCSharp_DebugTracing_Exception_ForLoop(string source, ExpectedPauseEventStep[] actions)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(source);

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>();
            foreach (ExpectedPauseEventStep step in actions)
                controls.Add(step);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 5));
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

        [Test, TestCaseSource(nameof(GetDebugActionCases))]
        public void TestCSharp_DebugTracing_Exception_StepIn_DoNotPauseOnException(DebugAction action)
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error()
{
    throw new Exception(""Handled Error"");     // LINE 5
}
func_test_error();                              // LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, action),
            };
            controls.SetPauseOnExceptionPolicy(DebugPauseOnExceptionPolicy.PauseOnNone);
            controls.Breakpoints.Add(new CodeReferenceBreakpoint(code, 7));
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

        [Test]
        public void TestCSharp_DebugTracing_Exception_StopDebug()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
void func_test_error()
{
    throw new Exception(""Handled Error"");     // LINE 5
}
func_test_error();                              // LINE 7
");

            var controls = new DebugPauseDetectorControls<ExpectedPauseEventStep>
            {
                new ( 7, ExecEvent.Line, DebugAction.StepIn),
                new ( 5, ExecEvent.Line, DebugAction.StepOver),
                new ( 5, ExecEvent.Exception, DebugAction.Stop),
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

            Assert.Throws<DebugStopException>(() => code.Debug(new DebugContext()));
        }

        static readonly Regex s_sourceIdFinder = new(@"\[Rhino.Runtime.Code.Execution.SourceFrameAttribute\((?<id>.+?)\)\]");
        static readonly Regex s_traceFinder = new(@"Rhino.+?RoslynTracer.Trace\(.+?,\s(?<trace>\d+),\s(?<event>\d+),.+?\);");

        static string GetDebugTracedSource(Code code)
        {
            code.Text.TryGetTransformed(new DebugContext(), out string tracedSource);
            tracedSource = s_sourceIdFinder.Replace(tracedSource, "[ID($1)]");
            tracedSource = s_traceFinder.Replace(tracedSource, "TRACE($1,$2);");
            return tracedSource;
        }

        [Test]
        public void TestCSharp_DebugTraceInsert_ForLoop()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
int total = 0;
for(int i =0; i < 3; i++)
{
    total += i;
}");

            string tracedSource = GetDebugTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"
sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){
TRACE(2,0);TRACE(2,1);int total = 0;
{object __roslynloopcache__i__ = default;bool __roslynloopstop__0__ = false;TRACE(3,1);for(int i =0; i < 3; i++)
{
__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;if(__roslynloopstop__0__)TRACE(3,1);__roslynloopstop__0__ = true;__roslynloopcache__i__ = i;    TRACE(5,1);total += i;
}TRACE(3,1);}
}
}

", tracedSource);
        }

        [Test]
        public void TestCSharp_DebugTraceInsert_ForLoop_Nested()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
int total = 0;
for(int i = 0; i < 3; i++)
    for (int j = 0; j < 2; j++)
        total += i + j;
");

            string tracedSource = GetDebugTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"
sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){
TRACE(2,0);TRACE(2,1);int total = 0;
{object __roslynloopcache__i__ = default;bool __roslynloopstop__0__ = false;TRACE(3,1);for(int i = 0; i < 3; i++)
{__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;if(__roslynloopstop__0__)TRACE(3,1);__roslynloopstop__0__ = true;__roslynloopcache__i__ = i;    TRACE(4,1);{object __roslynloopcache__j__ = default;bool __roslynloopstop__1__ = false;    TRACE(4,1);for (int j = 0; j < 2; j++)
{__roslynloopcache__j__ = __roslynloopcache__j__ ?? j;if(__roslynloopstop__1__)TRACE(4,1);__roslynloopstop__1__ = true;__roslynloopcache__j__ = j;        TRACE(5,1);total += i + j;
}TRACE(4,1);}}TRACE(3,1);}
}
}

", tracedSource);
        }

        [Test]
        public void TestCSharp_DebugTraceInsert_ForLoop_Multi()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
int total = 0;
for(int i =0,j = 1; i < 3; i++, j++)
{
    total += i;
    total += j;
}");

            string tracedSource = GetDebugTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"
sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){
TRACE(2,0);TRACE(2,1);int total = 0;
{object __roslynloopcache__i__ = default;object __roslynloopcache__j__ = default;bool __roslynloopstop__0__ = false;TRACE(3,1);for(int i =0,j = 1; i < 3; i++, j++)
{
__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;__roslynloopcache__j__ = __roslynloopcache__j__ ?? j;if(__roslynloopstop__0__)TRACE(3,1);__roslynloopstop__0__ = true;__roslynloopcache__i__ = i;__roslynloopcache__j__ = j;    TRACE(5,1);total += i;
    TRACE(6,1);total += j;
}TRACE(3,1);}
}
}

", tracedSource);
        }

        [Test]
        public void TestCSharp_DebugTraceInsert_ForEachLoop()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using System.Linq;

int total = 0;
foreach (int i in Enumerable.Range(0, 3))
{
    total += i;
}
int f = total;");

            string tracedSource = GetDebugTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"
using System;
using System.Linq;

sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){
TRACE(5,0);TRACE(5,1);int total = 0;
{object __roslynloopcache__i__ = default;bool __roslynloopstop__0__ = false;TRACE(6,1);foreach (int i in Enumerable.Range(0, 3))
{
__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;if(__roslynloopstop__0__)TRACE(6,1);__roslynloopstop__0__ = true;__roslynloopcache__i__ = i;    TRACE(8,1);total += i;
}TRACE(6,1);}TRACE(10,1);int f = total;TRACE(10,2);
}
}

", tracedSource);
        }

        [Test]
        public void TestCSharp_DebugTraceInsert_ForEachLoop_Nested()
        {
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(
@"
using System;
using System.Linq;

int total = 0;
foreach (int i in Enumerable.Range(0, 3))
    foreach (int j in Enumerable.Range(0, 3))
        total += i + j;
");

            string tracedSource = GetDebugTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"
using System;
using System.Linq;

sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){
TRACE(5,0);TRACE(5,1);int total = 0;
{object __roslynloopcache__i__ = default;bool __roslynloopstop__0__ = false;TRACE(6,1);foreach (int i in Enumerable.Range(0, 3))
{__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;if(__roslynloopstop__0__)TRACE(6,1);__roslynloopstop__0__ = true;__roslynloopcache__i__ = i;    TRACE(7,1);{object __roslynloopcache__j__ = default;bool __roslynloopstop__1__ = false;    TRACE(7,1);foreach (int j in Enumerable.Range(0, 3))
{__roslynloopcache__j__ = __roslynloopcache__j__ ?? j;if(__roslynloopstop__1__)TRACE(7,1);__roslynloopstop__1__ = true;__roslynloopcache__j__ = j;        TRACE(8,1);total += i + j;
}TRACE(7,1);}}TRACE(6,1);}
}
}

", tracedSource);
        }
#endif

#if RC8_19
        [Test]
        public void TestCSharp_Complete_NoProgram()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            string[] names = completions.Select(c => c.Text).ToArray();

            Assert.False(names.Contains("Program"));
            Assert.False(names.Contains("string[] args"));
        }

        [Test]
        public void TestCSharp_Complete_Methods()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

Plane p;

p.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            HashSet<CompletionKind> kinds = completions.Select(c => c.Kind).ToHashSet();

            Assert.True(kinds.Contains(CompletionKind.Method));
        }

        [Test]
        public void TestCSharp_Complete_NoMethods()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            HashSet<CompletionKind> kinds = completions.Select(c => c.Kind).ToHashSet();

            Assert.False(kinds.Contains(CompletionKind.Method));
        }

        [Test]
        public void TestCSharp_Complete_NoComment()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;
// some comment (leading trivia)
Line m = new ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @"
// some other comment after (trailing trivia)
");

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci = completions[0];
            Assert.AreEqual("Line", ci.Text);
            Assert.AreEqual(CompletionKind.Struct, ci.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Usings_Order()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            string[] names = completions.Select(c => c.Text).ToArray();

            Assert.GreaterOrEqual(completions.Count(), 7);

            Assert.AreEqual(nameof(System), names.ElementAt(0));
            Assert.AreEqual(nameof(Rhino), names.ElementAt(1));
            Assert.AreEqual(nameof(Grasshopper), names.ElementAt(2));
            Assert.AreEqual(nameof(GH_IO), names.ElementAt(3));
            Assert.AreEqual("Eto", names.ElementAt(4));
            Assert.AreEqual(nameof(RhinoCodePlatform), names.ElementAt(5));
            Assert.AreEqual(nameof(Microsoft), names.ElementAt(6));
        }

        [Test]
        public void TestCSharp_Complete_Usings_Excluded()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            string[] names = completions.Select(c => c.Text).ToArray();

            Assert.False(names.Contains("Clipper"));
            Assert.False(names.Contains("ClipperLib"));
            Assert.False(names.Contains("Mono"));
            Assert.False(names.Contains("MonoMac"));
            Assert.False(names.Contains("MonomacTestConversion"));
            Assert.False(names.Contains("Internal"));
        }

        [Test]
        public void TestCSharp_Complete_Usings_OnlyNamespaces()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;
using ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();

            Assert.IsNotEmpty(completions);

            Assert.True(completions.All(c => c.Kind == CompletionKind.Module));
        }

        static IEnumerable<TestCaseData> GetUsingsOnlyImportedCases()
        {
            yield return new(@"// #! csharp
using Rhino.Geometry;
")
            { TestName = nameof(TestCSharp_Complete_Usings_OnlyImported) + $"_Column1" };

            yield return new(@"// #! csharp
using Rhino.Geometry;
 ")
            { TestName = nameof(TestCSharp_Complete_Usings_OnlyImported) + $"_Column2" };
        }

        [Test, TestCaseSource(nameof(GetUsingsOnlyImportedCases))]
        public void TestCSharp_Complete_Usings_OnlyImported(string s)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();

            Assert.IsNotEmpty(completions);

            Assert.Contains("Arc", names);
            Assert.Contains("ArcCurve", names);

            Assert.Contains("if", names);
            Assert.Contains("foreach", names);
            Assert.Contains("while", names);

            Assert.False(names.Contains("Action"));
            Assert.False(names.Contains("Console"));
        }

        static IEnumerable<TestCaseData> GetCompleteObjectCreationCases()
        {
            yield return new(@"// #! csharp
using Rhino.Geometry;
Line m = new ", "Line", CompletionKind.Struct)
            { TestName = nameof(TestCSharp_Complete_ObjectCreation_One) + $"_OneAssignment" };

            yield return new(@"// #! csharp
using Rhino.Geometry;
int f = 12; Line m = new ", "Line", CompletionKind.Struct)
            { TestName = nameof(TestCSharp_Complete_ObjectCreation_One) + $"_TwoAssignments" };

            yield return new(@"// #! csharp
using Rhino.Geometry;
System.Console.Write(); Line m = new ", "Line", CompletionKind.Struct)
            { TestName = nameof(TestCSharp_Complete_ObjectCreation_One) + $"_CallAndOneAssignment" };

            yield return new(@"// #! csharp
using Rhino.Geometry;
Plane p = new ", "Plane", CompletionKind.Struct)
            { TestName = nameof(TestCSharp_Complete_ObjectCreation_One) + $"_OneAssignment_Plane" };
        }

        [Test, TestCaseSource(nameof(GetCompleteObjectCreationCases))]
        public void TestCSharp_Complete_ObjectCreation_One(string s, string expectedText, CompletionKind expectedKind)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci = completions[0];
            Assert.AreEqual(expectedText, ci.Text);
            Assert.AreEqual(expectedKind, ci.Kind);
        }

        [Test]
        public void TestCSharp_Complete_ObjectCreation_NotImported_List()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;
List<int> f = new ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.False(completions.Select(c => c.Text).Contains("List"));
        }

        [Test]
        public void TestCSharp_Complete_ObjectCreation_Imported_List()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections.Generic;
using Rhino.Geometry;
List<int> f = new ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci = completions[0];
            Assert.AreEqual("List<T>", ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual("List", ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_ObjectCreation_Imported_IList()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections;
using System.Collections.Generic;
using Rhino.Geometry;
IList t = new ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 5);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(System.Collections.ArrayList), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);

            ci = completions[1];
            Assert.AreEqual(nameof(System.Collections.CollectionBase), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);

            ci = completions[2];
            Assert.AreEqual(nameof(Rhino.Geometry.Interpolator), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);

            ci = completions[3];
            Assert.AreEqual("List<T>", ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual("List", ci.CommitText);

            ci = completions[4];
            Assert.AreEqual(nameof(Rhino.Geometry.Polyline), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
        }

        [Test]
        public void TestCSharp_Complete_ObjectCreation_Imported_Var()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections;
using System.Collections.Generic;
using Rhino.Geometry;
var m = new ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo ci;

            ci = completions.First(c => c.Text == "BitArray");
            Assert.AreEqual(nameof(System.Collections.BitArray), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(System.Collections.BitArray), ci.CommitText);

            ci = completions.First(c => c.Text == "KeyValuePair");
            Assert.AreEqual(nameof(System.Collections.Generic.KeyValuePair), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(System.Collections.Generic.KeyValuePair), ci.CommitText);

            ci = completions.First(c => c.Text == "ArcCurve");
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_ObjectCreation_AcceptedKinds()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections;
using System.Collections.Generic;
using Rhino.Geometry;
var m = new ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            CompletionKind[] kinds = completions.Select(c => c.Kind).ToHashSet().ToArray();

            Assert.Contains(CompletionKind.Class, kinds);
            Assert.Contains(CompletionKind.Struct, kinds);
            Assert.Contains(CompletionKind.Module, kinds);

            Assert.False(kinds.Contains(CompletionKind.Method));
            Assert.False(kinds.Contains(CompletionKind.Interface));
            Assert.False(kinds.Contains(CompletionKind.Enum));
            Assert.False(kinds.Contains(CompletionKind.Constant));
            Assert.False(kinds.Contains(CompletionKind.Variable));
            Assert.False(kinds.Contains(CompletionKind.Value));
        }

        [Test]
        public void TestCSharp_Complete_Assignment_Imported_IList()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections;
using System.Collections.Generic;
using Rhino.Geometry;
IList t = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 7);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(System.Collections.ArrayList), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);

            ci = completions[1];
            Assert.AreEqual(nameof(System.Collections.CollectionBase), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);

            ci = completions[2];
            Assert.AreEqual(nameof(System.Collections.IList), ci.Text);
            Assert.AreEqual(CompletionKind.Interface, ci.Kind);

            ci = completions[3];
            Assert.AreEqual(nameof(System.Collections.IList) + "<T>", ci.Text);
            Assert.AreEqual(CompletionKind.Interface, ci.Kind);
            Assert.AreEqual(nameof(System.Collections.IList), ci.CommitText);

            ci = completions[4];
            Assert.AreEqual(nameof(Rhino.Geometry.Interpolator), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);

            ci = completions[5];
            Assert.AreEqual("List<T>", ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual("List", ci.CommitText);

            ci = completions[6];
            Assert.AreEqual(nameof(Rhino.Geometry.Polyline), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Assignment_Imported_ImmutableDictionary()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections.Immutable;
using Rhino.Geometry;
ImmutableDictionary<int, int> d = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 2);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(System.Collections.Immutable.ImmutableDictionary), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(System.Collections.Immutable.ImmutableDictionary), ci.CommitText);

            ci = completions[1];
            Assert.AreEqual(nameof(System.Collections.Immutable.ImmutableDictionary) + "<TKey, TValue>", ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(System.Collections.Immutable.ImmutableDictionary), ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_Assignment_Imported_Enum()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections;
using System.Collections.Generic;
using Rhino.Geometry;
Continuity c = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 14);

            string n;
            CompletionInfo ci;

            n = nameof(Rhino.Geometry.Continuity);
            ci = completions[0];
            Assert.AreEqual(n, ci.Text);
            Assert.AreEqual(CompletionKind.Enum, ci.Kind);
            Assert.AreEqual(n, ci.CommitText);

            n = nameof(Rhino.Geometry.Continuity) + '.' + nameof(Rhino.Geometry.Continuity.C1_continuous);
            ci = completions.First(c => c.Text == n);
            Assert.AreEqual(n, ci.Text);
            Assert.AreEqual(CompletionKind.EnumMember, ci.Kind);
            Assert.AreEqual(n, ci.CommitText);

            n = nameof(Rhino.Geometry.Continuity) + '.' + nameof(Rhino.Geometry.Continuity.C2_locus_continuous);
            ci = completions.First(c => c.Text == n);
            Assert.AreEqual(n, ci.Text);
            Assert.AreEqual(CompletionKind.EnumMember, ci.Kind);
            Assert.AreEqual(n, ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_Assignment_FieldDeclaration()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

class MyClass
{
    Plane a =";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(Rhino.Geometry.Plane), ci.Text);
            Assert.AreEqual(CompletionKind.Struct, ci.Kind);
            Assert.AreEqual(nameof(Rhino.Geometry.Plane), ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_Assignment_PropertyDeclaration()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

class MyClass
{
    public ArcCurve M { get; set; }  = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_Assignment_Using()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

using(ArcCurve f = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_Assignment_Detect()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

ArcCurve m;

m = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_Assignment_Detect_Qualified()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

Rhino.Geometry.ArcCurve m;

m = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci;

            ci = completions[0];
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.Text);
            Assert.AreEqual(CompletionKind.Class, ci.Kind);
            Assert.AreEqual(nameof(Rhino.Geometry.ArcCurve), ci.CommitText);
        }

        [Test]
        public void TestCSharp_Complete_Enum()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Collections;
using System.Collections.Generic;
using Rhino.Geometry;
Continuity c = Continuity.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.AreEqual(13, completions.Length);
        }

        [Test]
        public void TestCSharp_Complete_AbstractType_ObjectDeclaration()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;
System.Drawing.";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c = completions.First(c => c.Text == "Brush");
            Assert.AreEqual("Brush", c.Text);
            Assert.AreEqual(CompletionKind.Class, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_AbstractType_ObjectAssignment()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System.Drawing;
using Rhino.Geometry;
System.Drawing.Brush b = ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            CompletionInfo c = completions.First(c => c.Text == "Brush");
            Assert.AreEqual("Brush", c.Text);
            Assert.AreEqual(CompletionKind.Class, c.Kind);
        }

        static IEnumerable<TestCaseData> GetCompleteKeywordsCases()
        {
            yield return new(@"// #! csharp
using Rhino.Geometry;
")
            { TestName = nameof(TestCSharp_Complete_Keywords) + "_Any" };

            yield return new(@"// #! csharp
using Rhino.Geometry;

class D {
    public void Test()
    { ")
            { TestName = nameof(TestCSharp_Complete_Keywords) + "_Class" };
        }

        [Test, TestCaseSource(nameof(GetCompleteKeywordsCases))]
        public void TestCSharp_Complete_Keywords(string s)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            CompletionInfo ci;

            ci = completions.First(c => c.Text == "this");
            Assert.AreEqual("this", ci.Text);
            Assert.AreEqual(CompletionKind.Keyword, ci.Kind);

            ci = completions.First(c => c.Text == "catch");
            Assert.AreEqual("catch", ci.Text);
            Assert.AreEqual(CompletionKind.Keyword, ci.Kind);

            ci = completions.First(c => c.Text == "unmanaged");
            Assert.AreEqual("unmanaged", ci.Text);
            Assert.AreEqual(CompletionKind.Keyword, ci.Kind);

            ci = completions.First(c => c.Text == "yield");
            Assert.AreEqual("yield", ci.Text);
            Assert.AreEqual(CompletionKind.Keyword, ci.Kind);
        }

        static IEnumerable<TestCaseData> GetCompleteNotKeywordsCases()
        {
            yield return new(@"// #! csharp
using ")
            { TestName = nameof(TestCSharp_Complete_Keywords) + "_Using" };

            yield return new(@"// #! csharp
using Rhino.Geometry;

Plane p = new ")
            { TestName = nameof(TestCSharp_Complete_Keywords) + "_ObjectCreation" };
        }

        [Test, TestCaseSource(nameof(GetCompleteNotKeywordsCases))]
        public void TestCSharp_Complete_NoKeywords(string s)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompletionInfo[] completions = CompleteAtPosition(code, s.Length).ToArray();
            string[] names = completions.Select(c => c.Text).ToArray();

            Assert.GreaterOrEqual(completions.Length, 1);

            Assert.False(names.Contains("this"));
            Assert.False(names.Contains("catch"));
            Assert.False(names.Contains("unmanaged"));
            Assert.False(names.Contains("yield"));
        }

        [Test]
        public void TestCSharp_Complete_LocalSymbols()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

Plane p;

";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = true };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c;

            c = completions.First(c => c.Text == "p");
            Assert.AreEqual("p", c.Text);
            Assert.AreEqual(CompletionKind.Variable, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_LocalSymbols_Function()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using Rhino.Geometry;

Plane p;

void Test(int argument)
{
    ";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = true };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c;

            c = completions.First(c => c.Text == "argument");
            Assert.AreEqual("argument", c.Text);
            Assert.AreEqual(CompletionKind.Variable, c.Kind);
        }

        protected const string KEYWORD_SORT_PREFIX = "zzkw_";

        [Test]
        public void TestCSharp_Complete_Snippet_On()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

i";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = true };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c;

            c = completions.First(c => c.Text == "if");
            Assert.AreEqual("if", c.Text);
            Assert.AreEqual(CompletionKind.Keyword, c.Kind);

            c = completions.First(c => c.SortText == KEYWORD_SORT_PREFIX + "if2");
            Assert.AreEqual("if", c.Text);
            Assert.AreEqual(CompletionKind.Snippet, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Snippet_Off()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

i";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = false };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            // NOTE:
            // monaco sorts completions and brings 'if' to the top.
            // we just find that completion to assert its kind as keyword
            CompletionInfo c = completions.First(c => c.Text == "if");
            Assert.AreEqual("if", c.Text);
            Assert.AreEqual(CompletionKind.Keyword, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Snippet_On_For_ForEach()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

f";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = true };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c;

            c = completions.First(c => c.Text == "for");
            Assert.AreEqual("for", c.Text);
            Assert.AreEqual(CompletionKind.Keyword, c.Kind);

            c = completions.First(c => c.SortText == KEYWORD_SORT_PREFIX + "for2");
            Assert.AreEqual("for", c.Text);
            Assert.AreEqual(CompletionKind.Snippet, c.Kind);

            c = completions.First(c => c.Text == "foreach");
            Assert.AreEqual("foreach", c.Text);
            Assert.AreEqual(CompletionKind.Keyword, c.Kind);

            c = completions.First(c => c.SortText == KEYWORD_SORT_PREFIX + "foreach2");
            Assert.AreEqual("foreach", c.Text);
            Assert.AreEqual(CompletionKind.Snippet, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Snippet_On_While()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

w";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = true };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c;

            c = completions.First(c => c.Text == "while");
            Assert.AreEqual("while", c.Text);
            Assert.AreEqual(CompletionKind.Keyword, c.Kind);

            c = completions.First(c => c.SortText == KEYWORD_SORT_PREFIX + "while2");
            Assert.AreEqual("while", c.Text);
            Assert.AreEqual(CompletionKind.Snippet, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Snippet_On_DoWhile()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

d";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = true };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c;

            c = completions.First(c => c.Text == "do");
            Assert.AreEqual("do", c.Text);
            Assert.AreEqual(CompletionKind.Keyword, c.Kind);

            c = completions.First(c => c.SortText == KEYWORD_SORT_PREFIX + "do2");
            Assert.AreEqual("do", c.Text);
            Assert.AreEqual(CompletionKind.Snippet, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Snippet_On_Switch()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

s";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + Environment.NewLine);

            CompleteOptions options = new() { Snippets = true };
            CompletionInfo[] completions = CompleteAtPosition(code, s.Length, options).ToArray();

            Assert.IsNotEmpty(completions);

            CompletionInfo c;

            c = completions.First(c => c.Text == "switch");
            Assert.AreEqual("switch", c.Text);
            Assert.AreEqual(CompletionKind.Keyword, c.Kind);

            c = completions.First(c => c.SortText == KEYWORD_SORT_PREFIX + "switch2");
            Assert.AreEqual("switch", c.Text);
            Assert.AreEqual(CompletionKind.Snippet, c.Kind);
        }

        [Test]
        public void TestCSharp_Complete_Signature_New()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

new Rhino.Geometry.Mesh(";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @")");

            SignatureInfo[] signatures = CompleteSignatureAtPosition(code, s.Length).ToArray();

            Assert.IsNotEmpty(signatures);

            SignatureInfo si;

            si = signatures.ElementAt(0);

            Assert.AreEqual("Mesh()", si.Text);
            Assert.AreEqual(0, si.Parameters.Length);
        }

        [Test]
        public void TestCSharp_Complete_Signature_NoNew()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

Rhino.Geometry.Mesh(";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @")");

            SignatureInfo[] signatures = CompleteSignatureAtPosition(code, s.Length).ToArray();

            Assert.IsEmpty(signatures);
        }

        [Test]
        public void TestCSharp_Complete_Signature_NewPlane()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86465
            string s = @"// #! csharp
using System;
using Rhino;

new Rhino.Geometry.Plane(";
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(s + @")");

            SignatureInfo[] signatures = CompleteSignatureAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(signatures.Length, 6);

            SignatureInfo si;

            si = signatures.ElementAt(0);
            Assert.AreEqual("Plane()", si.Text);
            Assert.AreEqual(0, si.Parameters.Length);

            si = signatures.ElementAt(1);
            Assert.AreEqual(1, si.Parameters.Length);

            si = signatures.ElementAt(2);
            Assert.AreEqual(2, si.Parameters.Length);
        }

        [Test]
        public void TestCSharp_Complete_Signature_ScriptInstance_This_RhinoDocument()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86458
            string s = @"// #! csharp
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object x, object y, ref object a)
    {
        this.RhinoDocument.ReadFileVersion(";
            var script = new Grasshopper1Script(s + @")
    }
}
");

            Code code = script.CreateCode();

            SignatureInfo[] signatures = CompleteSignatureAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(signatures.Length, 1);

            SignatureInfo si;

            si = signatures.ElementAt(0);
            Assert.AreEqual("Int32 ReadFileVersion()", si.Text);
            Assert.True(!string.IsNullOrWhiteSpace(si.Description));
            Assert.AreEqual(0, si.Parameters.Length);
        }

        [Test]
        public void TestCSharp_Complete_Signature_ScriptInstance_This_RhinoDocument_ObjectFindId()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86458
            string s = @"// #! csharp
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object x, object y, ref object a)
    {
        this.RhinoDocument.Objects.Find(";
            var script = new Grasshopper1Script(s + @")
    }
}
");

            Code code = script.CreateCode();

            SignatureInfo[] signatures = CompleteSignatureAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(signatures.Length, 2);

            SignatureInfo si;

            si = signatures.ElementAt(0);
            Assert.True(si.Text.Contains("Find(Guid objectId)"));
            Assert.True(!string.IsNullOrWhiteSpace(si.Description));
            Assert.AreEqual(1, si.Parameters.Length);

            si = signatures.ElementAt(1);
            Assert.True(si.Text.Contains("Find(UInt32 runtimeSerialNumber)"));
            Assert.True(!string.IsNullOrWhiteSpace(si.Description));
            Assert.AreEqual(1, si.Parameters.Length);
        }

        [Test]
        public void TestCSharp_Complete_Signature_ScriptInstance_This_Component_AddedToDocument()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-86458
            string s = @"// #! csharp
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(object x, object y, ref object a)
    {
        this.Component.AddedToDocument(";
            var script = new Grasshopper1Script(s + @")
    }
}
");
            Code code = script.CreateCode();

            SignatureInfo[] signatures = CompleteSignatureAtPosition(code, s.Length).ToArray();

            Assert.GreaterOrEqual(signatures.Length, 1);

            SignatureInfo si;

            si = signatures.ElementAt(0);
            Assert.True(si.Text.Contains("void AddedToDocument(GH_Document document)"));
            Assert.True(!string.IsNullOrWhiteSpace(si.Description));
            Assert.AreEqual(1, si.Parameters.Length);
        }
#endif

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"cs\", "test_*.cs");
    }
}
