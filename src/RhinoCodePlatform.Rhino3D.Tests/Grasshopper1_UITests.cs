using System;
using System.Linq;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

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

using GHP = RhinoCodePluginGH;
using System.Text.RegularExpressions;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Grasshopper1_UITests : ScriptFixture
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
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.True(inputs[0].Name == "u");
            Assert.True(inputs[0].ValueType.Name == "int");

            Assert.True(inputs[1].Name == "v");
            Assert.True(inputs[1].ValueType.Name == "Point3d");

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
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.True(inputs[0].Name == "u");
            Assert.True(inputs[1].Name == "v");
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
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.True(inputs[0].Name == "u");
            Assert.True(inputs[0].ValueType.Name == "int");

            Assert.True(inputs[1].Name == "v");
            Assert.True(inputs[1].ValueType.Name == "double");

            // assert outputs
            ScriptParam[] outputs = script.Outputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.True(outputs[0].Name == "w");
            Assert.True(outputs[0].ValueType.Name == "Point3d");

            Assert.True(outputs[1].Name == "z");
            Assert.True(outputs[1].ValueType.Name == "Surface");

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
", updatedText);
            // assert inputs
            ScriptParam[] inputs = script.Inputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.True(inputs[0].Name == "x");
            Assert.True(inputs[0].ValueType.Name == "int");

            // assert outputs
            ScriptParam[] outputs = script.Outputs.Select(i => i.CreateScriptParam()).ToArray();
            Assert.True(outputs[0].Name == "a");
            Assert.True(outputs[0].ValueType.Name == "double");

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
            Assert.True(inputs[0].Name == "x");
            Assert.True(inputs[0].ValueType.Name == "string");

            Assert.True(inputs[1].Name == "y");
            Assert.True(inputs[1].ValueType.Name == "Point3d");

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
            Assert.True(inputs[0].Name == "x");
            Assert.True(inputs[0].ValueType.Name == "string");

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
            Assert.True(inputs[0].Name == "x");
            Assert.True(inputs[0].ValueType.Name == "GumballMode");

            Assert.True(inputs[1].Name == "y");
            Assert.True(inputs[1].ValueType.Name == "Decoder");

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
            GH_Document ghdoc = Grasshopper.Instances.DocumentServer.AddDocument(ghfile, makeActive: true);

            IGH_Component component = (IGH_Component)ghdoc.Objects.First(c => c.NickName == "PTS");
            IScriptAttribute cattribs = (IScriptAttribute)component.Attributes;
            ProgressReporterAttribs attribs =
                new(cattribs,
                    new Regex(@"Installing ""scipy"".+" +
                              @"Collecting scipy.+" +
                              @"Collecting numpy.+" +
                              @"Installing collected packages: numpy, scipy.+" +
                              @"Successfully installed numpy.+scipy.+", RegexOptions.Singleline));

            ghdoc.Enabled = true;

            RhinoCode.ReportProgressToConsole = false;
            ghdoc.NewSolution(expireAllObjects: true);
            RhinoCode.ReportProgressToConsole = true;

            Assert.IsTrue(attribs.Pass);
        }
#endif
    }
}
