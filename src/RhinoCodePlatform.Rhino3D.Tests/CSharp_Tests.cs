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
            { TestName = nameof(TestCSharp_DebugTracing_StackWatch_Function_L2) + "_CompactBrace" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StackWatch_Function_L2) + "_ExpandedBrace" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StackWatch_Function_L2) + "_ExpandedBraceSameLine" };
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
", INDEX_VAR, SUM_VAR) { TestName = nameof(TestCSharp_DebugTracing_LoopVariable) + "_ForLoop" };

            yield return new($@"
using System;
using System.Linq;

int {SUM_VAR} = 0;
foreach (int {INDEX_VAR} in Enumerable.Range(0, 3)) // line 6
{{
    {SUM_VAR} += {INDEX_VAR};   // line 8
}}
", INDEX_VAR, SUM_VAR) { TestName = nameof(TestCSharp_DebugTracing_LoopVariable) + "_ForEachLoop" };
        }

        [Test, TestCaseSource(nameof(GetLoopVariableSources))]
        public void TestCSharp_DebugTracing_LoopVariable(string source, string index, string sum)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85276
            Code code = GetLanguage(LanguageSpec.CSharp).CreateCode(source);

            DebugExpressionVariableResult getIndex(ExecFrame frame)
            {
                return frame.Evaluate().OfType<DebugExpressionVariableResult>().FirstOrDefault(r => r.Value.Id == index);
            }

            DebugExpressionVariableResult getSum(ExecFrame frame)
            {
                return frame.Evaluate().OfType<DebugExpressionVariableResult>().FirstOrDefault(r => r.Value.Id == sum);
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
                                DebugExpressionVariableResult er0 = getIndex(frame);
                                // csharp does not stop twice on for loop so
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

        static IEnumerable<TestCaseData> GetStepOverForLoops()
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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForLoop) + "_NoBlock" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForLoop) + "_NoBlockExpanded" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForLoop) + "_CompactBrace" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForLoop) + "_ExpandedBrace" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForLoop) + "_ExpandedBraceSameLine" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForLoop) + "_WithVariableInLoopScope" };
        }

        [Test, TestCaseSource(nameof(GetStepOverForLoops))]
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

        static IEnumerable<TestCaseData> GetStepOverForEachLoops()
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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForEachLoop) + "_NoBlock" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForEachLoop) + "_NoBlockExpanded" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForEachLoop) + "_CompactBrace" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForEachLoop) + "_ExpandedBrace" };

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
            { TestName = nameof(TestCSharp_DebugTracing_StepOver_ForEachLoop) + "_ExpandedBraceSameLine" };
        }

        [Test, TestCaseSource(nameof(GetStepOverForEachLoops))]
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

        static IEnumerable<TestCaseData> GetDebugActions()
        {
            yield return new(DebugAction.Continue);
            yield return new(DebugAction.StepIn);
            yield return new(DebugAction.StepOut);
            yield return new(DebugAction.StepOver);
        }

        [Test, TestCaseSource(nameof(GetDebugActions))]
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

        [Test, TestCaseSource(nameof(GetDebugActions))]
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

        [Test, TestCaseSource(nameof(GetDebugActions))]
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

        static IEnumerable<TestCaseData> GetDebugTracingExceptionForLoop()
        {
            foreach (DebugAction action in GetDebugActions().Select(da => (DebugAction)da.Arguments[0]))
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
                { TestName = nameof(GetDebugTracingExceptionForLoop) + $"_Compact_{action}" };

            foreach (DebugAction action in GetDebugActions().Select(da => (DebugAction)da.Arguments[0]))
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
                { TestName = nameof(GetDebugTracingExceptionForLoop) + $"_Expanded_{action}" };
        }

        [Test, TestCaseSource(nameof(GetDebugTracingExceptionForLoop))]
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

        [Test, TestCaseSource(nameof(GetDebugActions))]
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

        static string GetTracedSource(Code code)
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

            string tracedSource = GetTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){

TRACE(2,0);TRACE(2,1);int total = 0;
{object __roslynloopcache__i__ = default;bool __roslynloopstop__ = false;TRACE(3,1);for(int i =0; i < 3; i++)
{
__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;if(__roslynloopstop__)TRACE(3,1);__roslynloopstop__ = true;__roslynloopcache__i__ = i;    TRACE(5,1);total += i;
}TRACE(3,1);}
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

            string tracedSource = GetTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){

TRACE(2,0);TRACE(2,1);int total = 0;
{object __roslynloopcache__i__ = default;object __roslynloopcache__j__ = default;bool __roslynloopstop__ = false;TRACE(3,1);for(int i =0,j = 1; i < 3; i++, j++)
{
__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;__roslynloopcache__j__ = __roslynloopcache__j__ ?? j;if(__roslynloopstop__)TRACE(3,1);__roslynloopstop__ = true;__roslynloopcache__i__ = i;__roslynloopcache__j__ = j;    TRACE(5,1);total += i;
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

            string tracedSource = GetTracedSource(code);
            // TestContext.Progress.WriteLine(tracedSource.Replace("\"", "\"\""));
            Assert.AreEqual(@"
using System;
using System.Linq;
sealed class __RhinoCodeScript__{[ID(""Main()"")]
public void __RunScript__(Rhino.Runtime.Code.IThisCode __this__,Rhino.Runtime.Code.Execution.RunContext __context__){

TRACE(5,0);TRACE(5,1);int total = 0;
{object __roslynloopcache__i__ = default;bool __roslynloopstop__ = false;TRACE(6,1);foreach (int i in Enumerable.Range(0, 3))
{
__roslynloopcache__i__ = __roslynloopcache__i__ ?? i;if(__roslynloopstop__)TRACE(6,1);__roslynloopstop__ = true;__roslynloopcache__i__ = i;    TRACE(8,1);total += i;
}TRACE(6,1);}TRACE(10,1);int f = total;TRACE(10,2);
}
}

", tracedSource);
        }
#endif

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"cs\", "test_*.cs");
    }
}
