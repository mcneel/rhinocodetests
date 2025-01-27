// NOTE:
// - Reference to RhinoCommmod.dll is added by default
// - Use // r "<assembly name>" to reference other assemblies
//       e.g. // r "System.Text.Json"
//       e.g. // r "path/to/your/Library.dll"
// - Use // r nuget "<package name>==<package version>" to install and reference nuget packages.
//   >= and > are also accepted instead of ==
//       e.g. // r nuget "RestSharp==106.12.0"
//       e.g. // r nuget "RestSharp>=106.10.1"
// - Use #r "nuget: <package name>, <package version>" to install and reference nuget packages.
//       e.g. #r "nuget: RestSharp, 106.11.7"

using System;
using Rhino.Runtime.Code.Storage;

bool test = true;

var u = new Uri("C:/dsfsdf/sdfds/fds%$^%#^#$45656456.fs");

var title = u.GetEndpointTitle();
test &= title == "fds%$^%#^#$45656456.fs";
// Console.WriteLine($"{title} -> {test}");

var ext = u.GetEndpointExt();
test &= ext == ".fs";
// Console.WriteLine($"{ext} -> {test}");

var noext = u.GetEndpointTitleNoExt();
test &= noext == "fds%$^%#^#$45656456";
// Console.WriteLine($"{noext} -> {test}");


// Console.WriteLine(test);
result = test;
