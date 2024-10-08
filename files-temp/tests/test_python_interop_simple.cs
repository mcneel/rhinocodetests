// #! csharp
using System;
using System.Linq;
using System.Collections.Generic;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Execution;


var run = new RunContext
{
    OverrideCodeParams = true,
    Outputs = {
        ["randoms"] = null,
    }
};


RhinoCode.RunScript(@"
#! python 3
# r: numpy

import numpy
randoms = list(numpy.random.rand(10))
", run);


if (run.Outputs.TryGet("randoms", out List<object> data))
{
    foreach(double d in data.Cast<double>())
    {
        RhinoApp.WriteLine(d.ToString());
    }
}
