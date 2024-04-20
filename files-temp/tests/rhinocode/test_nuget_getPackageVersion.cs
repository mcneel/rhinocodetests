// #! csharp
// r "NuGet.Versioning.dll"

using System;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;

void GetPackageVersions(PackageSpec spec)
{
    foreach (var v in NuGetEnvirons.User.GetPackageVersions(spec))
    {
        Console.WriteLine(v);
    }
}

GetPackageVersions(new PackageSpec("RhinoCommon==8.5"));
GetPackageVersions(new PackageSpec("RhinoCommon==8.5-rc"));
GetPackageVersions(new PackageSpec("RhinoCommon==8.5-wip"));
