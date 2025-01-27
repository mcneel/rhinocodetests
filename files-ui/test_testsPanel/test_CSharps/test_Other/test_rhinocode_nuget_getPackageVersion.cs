// #! csharp
// r "NuGet.Versioning.dll"

using System;
using System.Linq;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;

bool test = true;

void GetPackageVersions(PackageSpec spec, int count)
{
    var versions = NuGetEnvirons.User.GetPackageVersions(spec);
    // Console.WriteLine($"count: {versions.Count()}");
    test &= (versions.Count() == count);
}

GetPackageVersions(new PackageSpec("RhinoCommon==8.5"), 1);
GetPackageVersions(new PackageSpec("RhinoCommon==8.5-rc"), 6);
GetPackageVersions(new PackageSpec("RhinoCommon==8.5-wip"), 0);


if (!test)
throw new Exception();