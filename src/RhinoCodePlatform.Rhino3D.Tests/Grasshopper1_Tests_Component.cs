using System;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Diagnostics;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using GKP = Grasshopper.Kernel.Parameters;

#if RC8_11
using RhinoCodePlatform.GH;
using RhinoCodePlatform.GH.Context;
using LGH1 = RhinoCodePlatform.Rhino3D.Languages.GH1;
using ComponentConfigs = RhinoCodePlatform.GH.ScriptConfigs;
#else
using RhinoCodePlatform.Rhino3D.GH;
using IScriptParameter = RhinoCodePlatform.Rhino3D.GH.IScriptVariable;
#endif

using RhinoCodePlatform.Rhino3D.Testing;
using RhinoCodePlatform.Rhino3D.Languages.GH1;

using GHP = RhinoCodePluginGH;
using Rhino;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Grasshopper1_Tests_Component : GH1ScriptFixture
    {
#if RC8_8
        [Test]
        public void TestGH1_Component_ChangeLanguage()
        {
            IScriptObject script = GHP.Components.ScriptComponent.Create("Test", @"
#! python 3
import os
") as IScriptObject;

            Assert.True(LanguageSpec.Python3.Matches(script.LanguageSpec));

            script.Text = @"
// #! csharp
using System;
";

            Assert.True(LanguageSpec.CSharp.Matches(script.LanguageSpec));
        }

        [Test]
        public void TestGH1_Component_ChangeLanguage_Specific()
        {
            IScriptObject script = GHP.Components.Python3Component.Create("Test", @"
#! python 3
import os
") as IScriptObject;

            Assert.True(LanguageSpec.Python3.Matches(script.LanguageSpec));

            script.Text = @"
// #! csharp
using System;
";

            Assert.False(LanguageSpec.CSharp.Matches(script.LanguageSpec));
        }

        [Test]
        public void TestGH1_Component_ChangeLanguage_SetLanguageSpec()
        {
            IScriptObject script = GHP.Components.ScriptComponent.Create("Test", @"
#! python 3
import os
") as IScriptObject;

            script.LanguageSpec = LanguageSpec.CSharp;

            Assert.AreEqual(LanguageSpec.CSharp, script.LanguageSpec);
        }

        [Test]
        public void TestGH1_Component_ChangeLanguage_SetLanguageSpec_Specific()
        {
            IScriptObject script = GHP.Components.Python3Component.Create("Test", @"
#! python 3
import os
") as IScriptObject;

            Assert.Throws<GHP.Components.LanguageReadonlyException>(() => script.LanguageSpec = LanguageSpec.CSharp);
        }

        [Test]
        public void TestGH1_Component_Params_CaptureOutput()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.CaptureOutput = false;

            IGH_Component component = (IGH_Component)script;
            Assert.True(component.Params.Output[0].Name == "a");
        }

        [Test]
        public void TestGH1_Component_Params_ScriptInput()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.HasExternScript = true;

            IGH_Component component = (IGH_Component)script;
            Assert.True(component.Params.Input[0].Name == "script");
        }

        [Test]
        public void TestGH1_Component_Params_ScriptOutput()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.CaptureScript = true;

            IGH_Component component = (IGH_Component)script;
            Assert.True(component.Params.Output[0].Name == "script");
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python3()
        {
            IScriptObject script = GHP.Components.Python3Component.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, u: int, v: Rhino.Geometry.Point3d):
        return u + v
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam input;
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();

            input = inputs[0];
            Assert.AreEqual("u", input.Name);
            Assert.AreEqual("int", input.ValueType.Name);
            Assert.IsTrue(input.IsOptional);

            input = inputs[1];
            Assert.AreEqual("v", input.Name);
            Assert.AreEqual("Point3d", input.ValueType.Name);
            Assert.IsTrue(input.IsOptional);

            // assert param converters
            IScriptParameter u_param = script.Inputs.ElementAt(0);
            IScriptParameter v_param = script.Inputs.ElementAt(1);

            Assert.True(u_param.Converter is LGH1.Converters.IntConverter);
            Assert.True(v_param.Converter is LGH1.Converters.Point3dConverter);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python2()
        {
            IScriptObject script = GHP.Components.IronPython2Component.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, u, v):
        return u + v
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam input;
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();

            input = inputs[0];
            Assert.True(input.Name == "u");
            Assert.IsTrue(input.IsOptional);

            input = inputs[1];
            Assert.True(input.Name == "v");
            Assert.IsTrue(input.IsOptional);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(int u, double v, ref Point3d w, ref Surface z)
    {
    }
}
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam input;
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();

            input = inputs[0];
            Assert.AreEqual("u", input.Name);
            Assert.AreEqual("int", input.ValueType.Name);
            Assert.IsTrue(input.IsOptional);

            input = inputs[1];
            Assert.AreEqual("v", input.Name);
            Assert.AreEqual("double", input.ValueType.Name);
            Assert.IsTrue(input.IsOptional);

            // assert outputs
            ScriptParam[] outputs = script.Outputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("w", outputs[0].Name);
            Assert.AreEqual("Point3d", outputs[0].ValueType.Name);

            Assert.AreEqual("z", outputs[1].Name);
            Assert.AreEqual("Surface", outputs[1].ValueType.Name);

            // assert param converters
            IScriptParameter u_param = script.Inputs.ElementAt(0);
            IScriptParameter v_param = script.Inputs.ElementAt(1);
            IScriptParameter w_param = script.Outputs.ElementAt(0);
            IScriptParameter z_param = script.Outputs.ElementAt(1);

            Assert.True(u_param.Converter is LGH1.Converters.IntConverter);
            Assert.True(v_param.Converter is LGH1.Converters.DoubleConverter);
            Assert.True(w_param.Converter is LGH1.Converters.Point3dConverter);
            Assert.True(z_param.Converter is LGH1.Converters.SurfaceConverter);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python3_Empty()
        {
            IScriptObject script = GHP.Components.Python3Component.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return 0
";

            Assert.IsNotEmpty(script.Inputs);

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            Assert.IsFalse(script.HasErrors);
            Assert.IsEmpty(script.Inputs);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python2_Empty()
        {
            IScriptObject script = GHP.Components.IronPython2Component.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self):
        return 0
";

            Assert.IsNotEmpty(script.Inputs);

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            Assert.IsFalse(script.HasErrors);
            Assert.IsEmpty(script.Inputs);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_Empty()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
    }
}
";
            Assert.IsNotEmpty(script.Inputs);
            Assert.IsNotEmpty(script.Outputs);

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            Assert.IsFalse(script.HasErrors);
            Assert.IsEmpty(script.Inputs);
            Assert.IsEmpty(script.Outputs);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_OutToRef()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(int x, out double a)
    {
        a = default;
    }
}
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            string updatedText = script.Text;

            Assert.AreEqual(@"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(int x, ref object a)
    {
        a = default;
    }
}
", EnsureCRLF(updatedText));
            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("x", inputs[0].Name);
            Assert.AreEqual("int", inputs[0].ValueType.Name);

            // assert outputs
            ScriptParam[] outputs = script.Outputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("a", outputs[0].Name);
            Assert.AreEqual("double", outputs[0].ValueType.Name);

            // assert param converters
            IScriptParameter x_param = script.Inputs.ElementAt(0);
            IScriptParameter a_param = script.Outputs.ElementAt(0);

            Assert.True(x_param.Converter is LGH1.Converters.IntConverter);
            Assert.True(a_param.Converter is LGH1.Converters.DoubleConverter);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python3_CorrectSelf()
        {
            IScriptObject script = GHP.Components.Python3Component.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript():
        return 0
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            Assert.IsTrue(script.Text.Contains("def RunScript(self)"));
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python2_CorrectSelf()
        {
            IScriptObject script = GHP.Components.IronPython2Component.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript():
        return 0
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            Assert.IsTrue(script.Text.Contains("def RunScript(self)"));

        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python3_Keep_Str_Hint()
        {
            // test python parameter changes does not overwrite 'str' hint
            IScriptObject script = GHP.Components.Python3Component.Create("Test", @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y):
        return x + y
") as IScriptObject;

            // add hint to parameter 'x'
            IScriptParameter x_param = script.Inputs.ElementAt(0);
            IScriptParameter y_param = script.Inputs.ElementAt(1);
            x_param.Converter = new LGH1.Converters.PythonStringConverter();

            // update script with parameter changes
            script.ParamsApply();

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x: str, y: Rhino.Geometry.Point3d):
        return x + y.X
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("x", inputs[0].Name);
            Assert.AreEqual("string", inputs[0].ValueType.Name);

            Assert.AreEqual("y", inputs[1].Name);
            Assert.AreEqual("Point3d", inputs[1].ValueType.Name);

            // verify parameter converters
            Assert.True(x_param.Converter is LGH1.Converters.PythonStringConverter);
            Assert.True(y_param.Converter is LGH1.Converters.Point3dConverter);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python2_Keep_Str_Hint()
        {
            // test python parameter changes does not overwrite 'str' hint
            IScriptObject script = GHP.Components.IronPython2Component.Create("Test", @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y):
        return x + y
") as IScriptObject;

            // add hint to parameter 'x'
            IScriptParameter x_param = script.Inputs.ElementAt(0);
            x_param.Converter = new LGH1.Converters.PythonStringConverter();

            // update script with parameter changes
            script.ParamsApply();

            // change the script
            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs
import math

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y, k):
        return x + y + k
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("x", inputs[0].Name);
            Assert.AreEqual("string", inputs[0].ValueType.Name);

            // verify parameter converters
            Assert.True(x_param.Converter is LGH1.Converters.PythonStringConverter);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_Wiring()
        {
            // tests input and output wiring updates correctly when
            // script parameters change and we do not end up with
            // orphaned wiring on the upstream or downstream components
            var doc = new GH_Document();

            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.CaptureOutput = false;

            // wire up the ins and outs
            IGH_Component component = (IGH_Component)script;
            IGH_Param number_in = new Param_Number();
            IGH_Param param_in = component.Params.Input[0];
            IGH_Param param_out = component.Params.Output[0];
            IGH_Param number_out = new Param_Number();
            doc.Objects.Add(component);
            doc.Objects.Add(number_in);
            doc.Objects.Add(number_out);

            param_in.AddSource(number_in);
            number_out.AddSource(param_out);

            Assert.True(number_in.SourceCount == 0);
            Assert.True(param_in.SourceCount == 1);
            Assert.True(number_out.SourceCount == 1);

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(int k)
    {
    }
}
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // verify wiring
            IGH_Param param_in_k = component.Params.Input[0];
            Assert.True(param_in_k.SourceCount == 0);
            Assert.True(number_out.SourceCount == 0);
        }

        [Test]
        public void TestGH1_Component_DirectCast_Converter()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
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
  private void RunScript(
	Rhino.UI.Gumball.GumballMode x,
	System.Text.Decoder y,
	ref object a)
  {
    a = null;
  }
}
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("x", inputs[0].Name);
            Assert.AreEqual("GumballMode", inputs[0].ValueType.Name);

            Assert.AreEqual("y", inputs[1].Name);
            Assert.AreEqual("Decoder", inputs[1].ValueType.Name);

            // assert param converters
            IScriptParameter x_param = script.Inputs.ElementAt(0);
            IScriptParameter y_param = script.Inputs.ElementAt(1);

            Assert.True(x_param.Converter is LGH1.Converters.CastConverter);
            Assert.True(y_param.Converter is LGH1.Converters.CastConverter);
        }

        [Test]
        public void TestGH1_Component_HasError()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test", @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(int u)
    {
") as IScriptObject;

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            Assert.True(script.HasErrors);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "gh1ui", "test_notkeepingvalue_rc8.8.ghx" })]
        public void TestGH1_Component_NotKeepingValue(string ghfile)
        {
            GH_Document ghdoc = Grasshopper.Instances.DocumentServer.AddDocument(ghfile, makeActive: true);

            ghdoc.Enabled = true;
            ghdoc.NewSolution(expireAllObjects: true);

            IGH_Param input = ((IGH_Param)ghdoc.Objects.FirstOrDefault(c => c.NickName == "Input"));
            ghdoc.RemoveObject(input, true);

            ghdoc.NewSolution(expireAllObjects: true);

            IGH_Param assert = ((IGH_Param)ghdoc.Objects.FirstOrDefault(c => c.NickName == "Assert"));
            Assert.IsFalse(assert.RuntimeMessages(GH_RuntimeMessageLevel.Error).Any());
        }
#endif

#if RC8_9
        [Test]
        public void TestGH1_Component_Python_DoesNotReset_Converter_Goo()
        {
            var converter = new LGH1.Converters.GooConverter();
            TestGH1_Component_Python_ResetConverter(converter, converter);
        }

        [Test]
        public void TestGH1_Component_Python_DoesNotReset_Converter_Dynamic()
        {
            var converter = new LGH1.Converters.PythonDynamicConverter();
            TestGH1_Component_Python_ResetConverter(converter, converter);
        }

        [Test]
        public void TestGH1_Component_Python_DoReset_Converter_Goo()
        {
            TestGH1_Component_Python_ResetConverter(new LGH1.Converters.GooConverter(),
                                                    new LGH1.Converters.PythonDynamicConverter(),
                                                    DefaultOverrideKind.OverrideToExpected);
        }

        [Test]
        public void TestGH1_Component_Python_DoesNotReset_Converter_Float()
        {
            var converter = new LGH1.Converters.PythonFloatConverter();
            TestGH1_Component_Python_ResetConverter(converter, converter, DefaultOverrideKind.NoOverride);
        }

        [Test]
        public void TestGH1_Component_Python_DoesNotReset_Converter_Point3d()
        {
            var converter = new LGH1.Converters.Point3dConverter();
            TestGH1_Component_Python_ResetConverter(converter, converter, DefaultOverrideKind.NoOverride);
        }

        enum DefaultOverrideKind { NoOverride, Override, OverrideToExpected, }

        static void TestGH1_Component_Python_ResetConverter(IParamValueConverter converter,
                                                            IParamValueConverter expected,
                                                            DefaultOverrideKind overrideKind = DefaultOverrideKind.Override)
        {
            IParamValueConverter defaultConverter = default;
            if (overrideKind > DefaultOverrideKind.NoOverride)
            {
#if RC8_11
                defaultConverter = LGH1.Grasshopper1.GetConfiguredPythonConverter();
#else
                defaultConverter = ComponentConfigs.Current.GetDefaultPythonHint();
#endif
                ComponentConfigs.Current.DefaultPythonHint =
                    overrideKind == DefaultOverrideKind.OverrideToExpected ? expected.Id.Id : converter.Id.Id;
            }

            // https://mcneel.myjetbrains.com/youtrack/issue/RH-82051
            IScriptObject script = GHP.Components.Python3Component.Create("Test", @"
a = str(type(x))
") as IScriptObject;

            IScriptParameter x_param = script.Inputs.ElementAt(0);
            x_param.Converter = converter;

            // build so there is a code to apply params to
            script.ReBuild();

            IGH_Component component = (IGH_Component)script;
            component.Params.RegisterOutputParam(new GHP.Parameters.ScriptVariableParam("z"));
            // zui calls this automatically when parameter is added
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            x_param = script.Inputs.ElementAt(0);
            Assert.IsInstanceOf(expected.GetType(), x_param.Converter);

            if (overrideKind > DefaultOverrideKind.NoOverride)
            {
                ComponentConfigs.Current.DefaultPythonHint = defaultConverter.Id.Id;
            }
        }
#endif

#if RC8_10
        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "gh1ui", "test_plugins_package_install_progress_single_rc8.10.ghx" }),
               TestCaseSource(nameof(GetTestScript), new object[] { "gh1ui", "test_plugins_package_install_progress_context_rc8.10.ghx" })]
        public void TestGH1_PublishedComponent_RestoreProgress(string ghfile)
        {
            // NOTE:
            // disable solutions to avoid new solution when adding document.
            // pass false to makeActive to avoid new solution when enabling solutions later.
            GH_Document.EnableSolutions = false;
            GH_Document ghdoc = Grasshopper.Instances.DocumentServer.AddDocument(ghfile, makeActive: false);
            IGH_Component component = (IGH_Component)ghdoc.Objects.First(c => c.NickName == "PTS");
            IScriptAttribute cattribs = (IScriptAttribute)component.Attributes;
            ProgressWatcher watcher = new(cattribs, new Regex(@"Successfully installed.+"));

            GH_Document.EnableSolutions = true;
            RhinoCode.ReportProgressToConsole = false;

            ghdoc.Enabled = true;
            ghdoc.NewSolution(expireAllObjects: true);

            RhinoCode.ReportProgressToConsole = true;

            Assert.IsTrue(watcher.Pass);
        }
#endif

#if RC8_11
        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_InputNamedOutLine()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83087
            // input parameter with name starting with 'out' or 'ref' should be recognized
            // as input and not an output!
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript(int outline, int reference, ref Point3d w)
    {
    }
}
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("outline", inputs[0].Name);
            Assert.AreEqual("int", inputs[0].ValueType.Name);

            Assert.AreEqual("reference", inputs[1].Name);
            Assert.AreEqual("int", inputs[1].ValueType.Name);

            // assert outputs
            ScriptParam[] outputs = script.Outputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("w", outputs[0].Name);
            Assert.AreEqual("Point3d", outputs[0].ValueType.Name);

            // assert param converters
            IScriptParameter outline_param = script.Inputs.ElementAt(0);
            IScriptParameter reference_param = script.Inputs.ElementAt(1);
            IScriptParameter w_param = script.Outputs.ElementAt(0);

            Assert.True(outline_param.Converter is LGH1.Converters.IntConverter);
            Assert.True(reference_param.Converter is LGH1.Converters.IntConverter);
            Assert.True(w_param.Converter is LGH1.Converters.Point3dConverter);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python3_Multiline()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83124
            IScriptObject script = GHP.Components.Python3Component.Create("Test", @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y):
        return
") as IScriptObject;

            // build so there is a code to apply params to
            script.ReBuild();

            IGH_Component component = (IGH_Component)script;
            // create a few long parameters to push RunScript signature to become multiline
            // zui calls .VariableParameterMaintenance this automatically when parameter is added
            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("z") { Access = GH_ParamAccess.list });
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("u") { Access = GH_ParamAccess.tree });
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("v") { Access = GH_ParamAccess.list });
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            Assert.AreEqual(@"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self,
            x,
            y,
            z: System.Collections.Generic.List[object],
            u: Grasshopper.DataTree[object],
            v: System.Collections.Generic.List[object]):
        return
", EnsureCRLF(script.Text));

            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("w"));
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            Assert.AreEqual(@"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self,
            x,
            y,
            z: System.Collections.Generic.List[object],
            u: Grasshopper.DataTree[object],
            v: System.Collections.Generic.List[object],
            w):
        return
", EnsureCRLF(script.Text));
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_Python3_Multiline_ConvertToScriptInstance()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83124
            IScriptObject script = GHP.Components.Python3Component.Create("Test", @"

") as IScriptObject;

            // build so there is a code to apply params to
            script.ReBuild();

            IGH_Component component = (IGH_Component)script;
            // create a few long parameters to push RunScript signature to become multiline
            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("z") { Access = GH_ParamAccess.list });
            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("u") { Access = GH_ParamAccess.tree });
            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("v") { Access = GH_ParamAccess.list });

            // zui calls this automatically when parameter is added
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            script.Text = @"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, x, y):
        return
";

            // build so there is a code to apply params to
            script.ReBuild();
            script.ParamsApply();

            Assert.AreEqual(@"
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs

class MyComponent(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self,
            x,
            y,
            z: System.Collections.Generic.List[object],
            u: Grasshopper.DataTree[object],
            v: System.Collections.Generic.List[object]):
        return
", EnsureCRLF(script.Text));
        }
#endif

#if RC8_13
        [Test]
        public void TestGH1_Component_ParamsExtract()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84020

            IGH_Param param;

            param = new LGH1.Converters.GooConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_GenericObject>(param);

            param = new LGH1.Converters.BooleanConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Boolean>(param);

            param = new LGH1.Converters.IntConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Integer>(param);

            param = new LGH1.Converters.StringConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_String>(param);

            param = new LGH1.Converters.AnyStringConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_String>(param);

            param = new LGH1.Converters.PythonStringConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_String>(param);

            param = new LGH1.Converters.PythonFloatConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Number>(param);

            param = new LGH1.Converters.DoubleConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Number>(param);

            param = new LGH1.Converters.ComplexConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Complex>(param);

            param = new LGH1.Converters.DateTimeConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Time>(param);

            param = new LGH1.Converters.ColorConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Colour>(param);

            param = new LGH1.Converters.FilePathConverter(new Uri(@"C:\test.file")).CreateParameter();
            Assert.IsInstanceOf<GKP.Param_FilePath>(param);

            param = new LGH1.Converters.Point3dConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Point>(param);

            param = new LGH1.Converters.Point3dListConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Point>(param);

            param = new LGH1.Converters.Vector3dConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Vector>(param);

            param = new LGH1.Converters.PlaneConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Plane>(param);

            param = new LGH1.Converters.IntervalConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Interval>(param);

            param = new LGH1.Converters.UVIntervalConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Interval2D>(param);

            param = new LGH1.Converters.GuidConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Guid>(param);

            param = new LGH1.Converters.BoxConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Box>(param);

            param = new LGH1.Converters.TransformConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Transform>(param);

            param = new LGH1.Converters.LineConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Line>(param);

            param = new LGH1.Converters.CircleConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Circle>(param);

            param = new LGH1.Converters.ArcConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Arc>(param);

            param = new LGH1.Converters.CurveConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Curve>(param);

            param = new LGH1.Converters.PolylineConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Curve>(param);

            param = new LGH1.Converters.Rectangle3dConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Rectangle>(param);

            param = new LGH1.Converters.MeshConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Mesh>(param);

            param = new LGH1.Converters.SurfaceConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Surface>(param);

            param = new LGH1.Converters.ExtrusionConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Extrusion>(param);

            param = new LGH1.Converters.SubDConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_SubD>(param);

            param = new LGH1.Converters.BrepConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Brep>(param);

            param = new LGH1.Converters.PointCloudConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_PointCloud>(param);

            param = new LGH1.Converters.GeometryBaseConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Geometry>(param);

            param = new LGH1.Converters.HatchConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Hatch>(param);

            param = new LGH1.Converters.TextEntityConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_TextEntity>(param);

            param = new LGH1.Converters.TextDotConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_TextDot>(param);

            param = new LGH1.Converters.LeaderConverter().CreateParameter();
            Assert.IsInstanceOf<GKP.Param_Leader>(param);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_Empty_WithParentsInCode()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private void RunScript()
    {
        using MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(""TestCommandArgsGH"");
        using MemoryMappedViewStream stream = mmf.CreateViewStream();
        BinaryWriter writer = new BinaryWriter(stream);
        writer.Write(Encoding.UTF8.GetBytes(""TRUE\n""));
    }
}
";
            Assert.IsNotEmpty(script.Inputs);
            Assert.IsNotEmpty(script.Outputs);

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            Assert.IsFalse(script.HasErrors);
            Assert.IsEmpty(script.Inputs);
            Assert.IsEmpty(script.Outputs);
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_Multiline()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
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
  private void RunScript(
	Rhino.UI.Gumball.GumballMode x,
	List<int> y,
	ref object a)
  {
    a = null;
  }
}
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("x", inputs[0].Name);
            Assert.AreEqual("GumballMode", inputs[0].ValueType.Name);

            Assert.AreEqual("y", inputs[1].Name);
            Assert.AreEqual("List<int>", inputs[1].ValueType.Name);
        }
#endif

#if RC8_15
        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_RunScriptIndent()
        {
            // https://github.com/mcneel/rhino/commit/1a06f12095ac2031197a3e264a0e89588e24ff39
            // https://github.com/mcneel/rhino/commit/0b1febc688eab7680746f6452e98d41ecda80aad
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"using System;
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
    private void RunScript(object x, object y, object z, ref object a)
    {
    }
}
";

            script.ParamsCollect();

            IScriptParameter x_param = script.Inputs.ElementAt(0);
            x_param.Access = ScriptParamAccess.Tree;

            IScriptParameter y_param = script.Inputs.ElementAt(1);
            y_param.Access = ScriptParamAccess.Tree;

            script.ParamsApply();

            Assert.AreEqual(@"using System;
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
    private void RunScript(
		DataTree<object> x,
		DataTree<object> y,
		object z,
		ref object a)
    {
    }
}
", script.Text);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "gh1ui", "test_csharp_runScriptAsync_rc8.15.ghx" })]
        public void TestGH1_Component_RunScript_CSharp_Async(string ghfile)
        {
            Test_ScriptWithWait(new Uri(ghfile), 3);
        }
#endif

#if RC8_16
        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_AsyncRunScript_CompileError()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85144
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private async void RunScript(int u, double v, ref Point3d w, ref Surface z)
    {
    }
}
";
            script.ReBuild();

            Assert.IsTrue(script.TryGetCode(out Code code));
            CompileException ex = Assert.Throws<CompileException>(() => code.Build(new BuildContext()));
            Diagnostic d = ex.Diagnosis.OrderBy(d => d.Severity).First();
            Assert.AreEqual(DiagnosticSeverity.Error, d.Severity);
            Assert.IsTrue(d.Message.Contains("Async methods cannot have ref, in or out parameters"));
        }

        [Test]
        public void TestGH1_Component_ParamsCollect_CSharp_AsyncRunScript()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85144
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private async void RunScript(int u, double v)
    {
    }
}
";

            // collect parameters from RunScript and apply to component
            script.ParamsCollect();

            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.AreEqual("u", inputs[0].Name);
            Assert.AreEqual("int", inputs[0].ValueType.Name);

            Assert.AreEqual("v", inputs[1].Name);
            Assert.AreEqual("double", inputs[1].ValueType.Name);

            // assert param converters
            IScriptParameter u_param = script.Inputs.ElementAt(0);
            IScriptParameter v_param = script.Inputs.ElementAt(1);

            Assert.True(u_param.Converter is LGH1.Converters.IntConverter);
            Assert.True(v_param.Converter is LGH1.Converters.DoubleConverter);
        }

        [Test]
        public void TestGH1_Component_ParamsApply_CSharp_AsyncRunScript()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85144
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private async void RunScript(object x, object y)
    {
    }
}
";

            script.ReBuild();
            script.ParamsCollect();

            IGH_Component component = (IGH_Component)script;
            // create a few long parameters to push RunScript signature to become multiline
            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("z") { Access = GH_ParamAccess.list });

            // zui calls this automatically when parameter is added
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            script.ReBuild();
            script.ParamsApply();

            Assert.AreEqual(@"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private async void RunScript(
		object x,
		object y,
		System.Collections.Generic.List<object> z)
    {
    }
}
", EnsureCRLF(script.Text));
        }

        [Test]
        public void TestGH1_Component_ParamsApply_CSharp_AsyncRunScript_CompileError()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85144
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // change the script
            script.Text = @"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private async void RunScript(object x, object y, ref object a)
    {
    }
}
";

            script.ReBuild();
            script.ParamsCollect();

            IGH_Component component = (IGH_Component)script;
            // create a few long parameters to push RunScript signature to become multiline
            component.Params.RegisterInputParam(new GHP.Parameters.ScriptVariableParam("z") { Access = GH_ParamAccess.list });

            // zui calls this automatically when parameter is added
            ((IGH_VariableParameterComponent)component).VariableParameterMaintenance();

            script.ReBuild();
            script.ParamsApply();

            Assert.AreEqual(@"
using System;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;

public class Script_Instance : GH_ScriptInstance
{
    private async void RunScript(
		object x,
		object y,
		System.Collections.Generic.List<object> z,
		ref object a)
    {
    }
}
", EnsureCRLF(script.Text));

            Assert.IsTrue(script.TryGetCode(out Code code));
            code.ExpireCache();

            script.ReBuild();

            CompileException ex = Assert.Throws<CompileException>(() => code.Build(new BuildContext()));
            Diagnostic d = ex.Diagnosis.OrderBy(d => d.Severity).First();
            Assert.AreEqual(DiagnosticSeverity.Error, d.Severity);
            Assert.IsTrue(d.Message.Contains("Async methods cannot have ref, in or out parameters"));
        }
#endif

        static string EnsureCRLF(string input) => input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
    }
}
