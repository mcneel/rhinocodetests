using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Registry;


bool test = true;

var versions = RhinoCode.Languages
                        .Select(l => l.Id.Version)
                        .OrderByDescending(v => v.ToVersion());

// csharp 9
test &= versions.First().Major == 9;

// markdown 0.30
test &= versions.Last().Major == 0;

// Console.WriteLine(test);
result = test;
