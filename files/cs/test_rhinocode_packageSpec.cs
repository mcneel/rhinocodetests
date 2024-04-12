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

string[] ordered = new string[]
{
    "Name<6",
    "Name<=6.3",
    "Name==7",
    "Name>=7",
    "Name==7.1",
    "Name==8",
};

int index = 0;
bool test = true;
foreach(var pkg in packages.OrderBy(p => p))
{
    test &= pkg.ToString() == ordered[index];
    index++;
}

// Console.WriteLine(test);
result = test;