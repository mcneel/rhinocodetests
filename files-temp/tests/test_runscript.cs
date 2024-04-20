using System;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;

string python2 = @"#! python2
x = a + b
";

string python3 = @"#! python3
x = a + b
";

string csharp = @"//#! csharp
x = (int)a + (int)b;
";

foreach(string script in new string[] { python2, python3, csharp })
{
    var ctx = new RunContext
    {
        // define the inputs
        Inputs = {
            ["a"] = 21,
            ["b"] = 21,
        },

        // define the outputs
        // set values to default
        Outputs = {
            ["x"] = -1,
            ["j"] = -1,
        }
    };

    RhinoCode.RunScript(script, ctx);

    if (ctx.Outputs.TryGet("x", out int x))
        RhinoApp.WriteLine(x.ToString());
    
    if (ctx.Outputs.TryGet("j", out int j))
        RhinoApp.WriteLine(j.ToString());
}
