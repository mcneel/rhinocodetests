// #! csharp
/*
    C# script in Rhino is using rhinocode api to run a python 3 script (in the same process)
    and grab an instance of a Python-defined type. C# script can access this instance as
    a dynamic object and accesses attributes on that instance. As long as py3Code is in scope,
    the  instance of PythonObject in python should remain accessible (otherwise it will be
    garbage collected by python runtime).
*/
using System;
using System.Linq;
using System.Collections.Generic;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Execution;

// UNPUBLISHED API -- DO NOT SHARE
// find python language
var py3Lang = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);

// create a python code
// the code defines a class and instantiates
// the instance can be captured in output
var py3Code = py3Lang.CreateCode(@"
class PythonObject:
    def __init__(self, data):
        self.data = data
    
    def foo(self):
        return self.data


__instance__ = PythonObject(42)
");

// create a run context
// this hold inputs and output values
var ctx = new RunContext
{
  OverrideCodeParams = true,

  // initialize output value
  Outputs = {
        ["__instance__"] = null,
    }
};

// run the code
py3Code.Run(ctx);

// grab the instance of python object, generated in python
if (ctx.Outputs.TryGet("__instance__", out dynamic inst))
{
  int data = (int)inst.data;
  int foo = (int)inst.foo();

  RhinoApp.WriteLine($"Data: {data}");
  RhinoApp.WriteLine($"Foo: {foo}");
}
