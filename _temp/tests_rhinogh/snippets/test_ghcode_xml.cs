// r "Grasshopper"
// r "GH_IO"
using System;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Execution;

using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

var gh = RhinoCode.Languages.QueryLatest(new LanguageSpec("*.*.grasshopper", "1"));
// Console.WriteLine(gh);

var code = gh.CreateCode(new Uri(@"C:\Users\ein\gits\rhino\src4\rhino4\Plug-ins\RhinoCodePlugins\tests_rhinogh\code.ghx"));
// Console.WriteLine(code);

var input = new GH_Structure<GH_Integer>();
input.Append(new GH_Integer(21));

var ctx = new RunContext
{
    // define the inputs
    Inputs = {
        ["input"] = input,
    },

    // define the outputs
    // set values to default
    Outputs = {
        ["result"] = -1,
    },

    Options = {
        ["grasshopper.runAsCommand"] = false
    }
};

code.Run(ctx);

if (ctx.Outputs.TryGet("result", out IGH_Structure j))
{
   //  RhinoApp.WriteLine(j.ToString());

    foreach(var p in j.Paths)
    {
        // RhinoApp.WriteLine(p.ToString());

        foreach(var d in j.get_Branch(p))
            RhinoApp.WriteLine(d.ToString());
    }
}