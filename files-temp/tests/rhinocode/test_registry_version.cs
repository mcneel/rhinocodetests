using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Registry;

var versions = new List<RegistryVersionedSpec.SpecVersion>();
foreach(var lang in RhinoCode.Languages)
    versions.Add(lang.Id.Version);

foreach(var version in versions.OrderByDescending(v => v.ToVersion()))
    Console.WriteLine(version);