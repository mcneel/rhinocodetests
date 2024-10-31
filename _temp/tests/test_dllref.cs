// Note:
// This project is work-in-progress and still in its infancy
// - Reference to RhinoCommmod.dll is added by default
// - Use // r "<assembly name>" to reference other assemblies
//       e.g. // r "System.Text.Json"
//       e.g. // r "path/to/your/Library.dll"
// - Use // r nuget "<package name>==<package version>" to install and reference
//   nuget packages. >= and > are also accepted instead of ==
//       e.g. // r nuget "RestSharp==106.12.0"
//       e.g. // r nuget "RestSharp>=106.10.1"

/// r "Mono.Cecil.dll"
/// r "Nancy"
// r "Nuget.ProjectModel"
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

foreach (var assm in AppDomain.CurrentDomain.GetAssemblies())
    Console.WriteLine(assm.GetName().FullName);

