#r "NuGet.Versioning"

using System;
using System.Linq;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;

using NuGet;

var olds = NuGetEnvirons.User.GetPackageVersions(new PackageSpec("RhinoCommon==7"));
foreach (NuGet.Versioning.NuGetVersion version in olds.Reverse().Take(5))
    Console.WriteLine(version.ToString());
