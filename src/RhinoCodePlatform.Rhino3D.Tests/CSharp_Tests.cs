using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

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
using System.Collections.Concurrent;


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
            LanguageLibrary library = csharp.CreateLibrary(new Uri(Path.Combine(fileDir, "cs", "test_library")));

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

            Assert.True(execSpec.TryGetAsync(out bool? isAsync));
            Assert.True(isAsync ?? false);
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

            Assert.True(execSpec.TryGetAsync(out bool? isAsync));
            Assert.True(isAsync ?? false);
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
            Assert.Contains("CurveFeature", names);
            Assert.Contains("MeshFeature", names);
        }

        [Test]
        public void TestCSharp_CompileGuard_Library()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84921
            ILanguage csharp = GetLanguage(LanguageSpec.CSharp);

            TryGetTestFilesPath(out string fileDir);
            LanguageLibrary library = csharp.CreateLibrary(new Uri(Path.Combine(fileDir, "cs", "test_library_compileguard")));

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
            LanguageLibrary library = csharp.CreateLibrary(new Uri(Path.Combine(fileDir, "cs", "test_library_compileguard_not")));

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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
            Assert.Contains("byte", names);
            Assert.Contains("char", names);
            Assert.Contains(nameof(Enum.HasFlag), names);
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

            IEnumerable<Ed.Common.CompletionItem> completions = CompleteAtEndingPeriod(code, s);

            Assert.IsNotEmpty(completions);

            string[] names = completions.Select(c => c.label).ToArray();
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

                Assert.AreEqual(ctx.Id, code.CurrentContext);
                Interlocked.Increment(ref counter);
            });

            Assert.AreEqual(THREAD_COUNT, counter);
        }
#endif

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"cs\", "test_*.cs");

#if RC8_15
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
#endif
    }
}
