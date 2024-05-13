using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Text;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using RhinoCodePlatform.Rhino3D.GH;

using GHP = RhinoCodePluginGH;
using Grasshopper.Kernel.Parameters;

namespace RhinoCodePlatform.Rhino3D.Tests
{
#if RC8_8
    [TestFixture]
    public class Grasshopper1_UITests : ScriptFixture
    {
        [Test]
        public void TestComponent_ChangeLanguage()
        {
            // TODO:
            // can not change the component language after it is setup
            IScriptObject script = GHP.Components.ScriptComponent.Create("Test", @"
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
        public void TestComponent_Params_CaptureOutput()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.CaptureOutput = false;

            IGH_Component component = (IGH_Component)script;
            Assert.True(component.Params.Output[0].Name == "a");
        }

        [Test]
        public void TestComponent_Params_ScriptInput()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.HasExternScript = true;

            IGH_Component component = (IGH_Component)script;
            Assert.True(component.Params.Input[0].Name == "script");
        }

        [Test]
        public void TestComponent_Params_ScriptOutput()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.CaptureScript = true;

            IGH_Component component = (IGH_Component)script;
            Assert.True(component.Params.Output[0].Name == "script");
        }

        [Test]
        public void TestComponent_ParamsCollect_Python3()
        {
            IScriptObject script = GHP.Components.Python3Component.Create("Test") as IScriptObject;

            // build so component parameters are applied to the script
            script.ReBuild();

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
            IScriptVariable u_param = script.Inputs.ElementAt(0);
            IScriptVariable v_param = script.Inputs.ElementAt(1);
            
            Assert.True(u_param.Converter is GH1.Converters.IntConverter);
            Assert.True(v_param.Converter is GH1.Converters.Point3dConverter);
        }

        [Test]
        public void TestComponent_ParamsCollect_Python2()
        {
            IScriptObject script = GHP.Components.IronPython2Component.Create("Test") as IScriptObject;

            // build so component parameters are applied to the script
            script.ReBuild();

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
        public void TestComponent_ParamsCollect_CSharp()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // build so component parameters are applied to the script
            script.ReBuild();

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
            IScriptVariable u_param = script.Inputs.ElementAt(0);
            IScriptVariable v_param = script.Inputs.ElementAt(1);
            IScriptVariable w_param = script.Outputs.ElementAt(0);
            IScriptVariable z_param = script.Outputs.ElementAt(1);

            Assert.True(u_param.Converter is GH1.Converters.IntConverter);
            Assert.True(v_param.Converter is GH1.Converters.DoubleConverter);
            Assert.True(w_param.Converter is GH1.Converters.Point3dConverter);
            Assert.True(z_param.Converter is GH1.Converters.SurfaceConverter);
        }

        [Test]
        public void TestComponent_ParamsCollect_Python3_Keep_Str_Hint()
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

            // build so component parameters are applied to the script
            script.ReBuild();

            // add hint to parameter 'x'
            IScriptVariable x_param = script.Inputs.ElementAt(0);
            IScriptVariable y_param = script.Inputs.ElementAt(1);
            x_param.Converter = new GH1.Converters.PythonStringConverter();

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
            Assert.True(x_param.Converter is GH1.Converters.PythonStringConverter);
            Assert.True(y_param.Converter is GH1.Converters.Point3dConverter);
        }

        [Test]
        public void TestComponent_ParamsCollect_Python2_Keep_Str_Hint()
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

            // build so component parameters are applied to the script
            script.ReBuild();

            // add hint to parameter 'x'
            IScriptVariable x_param = script.Inputs.ElementAt(0);
            x_param.Converter = new GH1.Converters.PythonStringConverter();

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
            Assert.True(x_param.Converter is GH1.Converters.PythonStringConverter);
        }

        [Test]
        public void TestComponent_ParamsCollect_CSharp_Wiring()
        {
            // tests input and output wiring updates correctly when
            // script parameters change and we do not end up with
            // orphaned wiring on the upstream or downstream components
            var doc = new GH_Document();

            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;
            script.CaptureOutput = false;

            // build so component parameters are applied to the script
            script.ReBuild();

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
        public void TestComponent_DirectCast_Converter()
        {
            IScriptObject script = GHP.Components.CSharpComponent.Create("Test") as IScriptObject;

            // build so component parameters are applied to the script
            script.ReBuild();

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
            IScriptVariable x_param = script.Inputs.ElementAt(0);
            IScriptVariable y_param = script.Inputs.ElementAt(1);

            Assert.True(x_param.Converter is GH1.Converters.CastConverter);
            Assert.True(y_param.Converter is GH1.Converters.CastConverter);
        }
    }
#endif
}
