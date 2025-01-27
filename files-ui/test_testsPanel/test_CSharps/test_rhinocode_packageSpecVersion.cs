using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;

var versions = new PackageVersion[] {
    new PackageVersion(6, 0),
    new PackageVersion(6, 1),
    new PackageVersion(6, 3),
    new PackageVersion(6, 10, 12),
    new PackageVersion(7, 1),
    new PackageVersion(7, 0),
    new PackageVersion(7, 0, 100),
    new PackageVersion(7, 2, 200),
    new PackageVersion(7, 4, 300),
    new PackageVersion(8, 0),
    new PackageVersion(8, 1),
    new PackageVersion(8, 3),
};

bool test = true;
PackageSpec pkg;

// Console.WriteLine("Finding Older than 7 (<)");
pkg = new PackageSpec("Name<7");
foreach(var ver in versions.Where(v => pkg.Matches(v)))
    // Console.WriteLine(ver);
    test &= ver.Major == 6;

// Console.WriteLine("\nFinding Older or Equal than 7.* (<=)");
pkg = new PackageSpec("Name<=7");
foreach(var ver in versions.Where(v => pkg.Matches(v)))
    // Console.WriteLine(ver);
    test &= ver.Major <= 7;

// Console.WriteLine("\nFinding 7.* (==)");
pkg = new PackageSpec("Name==7");
foreach(var ver in versions.Where(v => pkg.Matches(v)))
    // Console.WriteLine(ver);
    test &= ver.Major == 7;

// Console.WriteLine("\nFinding Newer or Equal than 7.* (>=)");
pkg = new PackageSpec("Name>=7");
foreach(var ver in versions.Where(v => pkg.Matches(v)))
    // Console.WriteLine(ver);
    test &= ver.Major >= 7;

// Console.WriteLine("\nFinding Newer than 7.2 (>=)");
pkg = new PackageSpec("Name>=7.2");
foreach(var ver in versions.Where(v => pkg.Matches(v)))
    // Console.WriteLine(ver);
    test &= (ver.Major == 7 && ver.Minor >= 2) || ver.Major == 8;


// Console.WriteLine(test);
result = test;