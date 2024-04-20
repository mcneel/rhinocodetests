using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;

var packages = new PackageSpec[] {
    new PackageSpec("Name==8"),
    new PackageSpec("Name==7.1"),
    new PackageSpec("Name==7"),
    new PackageSpec("Name>=7"),
    new PackageSpec("Name<6"),
    new PackageSpec("Name<=6.3"),
};

foreach(var pkg in packages.OrderBy(p => p))
    Console.WriteLine(pkg);