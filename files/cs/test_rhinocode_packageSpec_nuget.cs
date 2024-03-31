using System;
using System.Linq;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;


var pkgSpec = new PackageSpecifierByText();

bool TestNuGetRef(string refSpec, NuGetPackageSpec nugetSpec)
{
    var pkgs = pkgSpec.Specify(new TemplateScript("T", refSpec));
    var refNugetSpec = pkgs.Packages.First().PackageSpecs.First();
    return refNugetSpec.Equals(nugetSpec);
}

bool result = true;

// with #r ================================================================================================
// using ,
result &= TestNuGetRef("#r \"nuget: RestSharp,110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
result &= TestNuGetRef("# r \"nuget: RestSharp , 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using ==
result &= TestNuGetRef("#r \"nuget: RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
result &= TestNuGetRef("# r \"nuget: RestSharp == 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using >=
result &= TestNuGetRef("#r \"nuget: RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));
result &= TestNuGetRef("# r \"nuget: RestSharp >= 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));

// using <=
result &= TestNuGetRef("#r \"nuget: RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));
result &= TestNuGetRef("# r \"nuget: RestSharp <= 110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));

// using >
result &= TestNuGetRef("#r \"nuget: RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));
result &= TestNuGetRef("# r \"nuget: RestSharp > 110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));

// using <
result &= TestNuGetRef("#r \"nuget: RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));
result &= TestNuGetRef("# r \"nuget: RestSharp < 110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));

// no version
result &= TestNuGetRef("#r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));
result &= TestNuGetRef("# r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));

// with //r ===============================================================================================
// using ,
result &= TestNuGetRef("//r \"nuget: RestSharp,110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
result &= TestNuGetRef("// r \"nuget: RestSharp , 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using ==
result &= TestNuGetRef("//r \"nuget: RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
result &= TestNuGetRef("// r \"nuget: RestSharp == 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using >=
result &= TestNuGetRef("//r \"nuget: RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));
result &= TestNuGetRef("// r \"nuget: RestSharp >= 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));

// using <=
result &= TestNuGetRef("//r \"nuget: RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));
result &= TestNuGetRef("// r \"nuget: RestSharp <= 110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));

// using >
result &= TestNuGetRef("//r \"nuget: RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));
result &= TestNuGetRef("// r \"nuget: RestSharp > 110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));

// using <
result &= TestNuGetRef("//r \"nuget: RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));
result &= TestNuGetRef("// r \"nuget: RestSharp < 110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));

// no version
result &= TestNuGetRef("//r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));
result &= TestNuGetRef("// r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));


// legacy format ==========================================================================================
result &= TestNuGetRef("//r nuget \"RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
result &= TestNuGetRef("//r nuget \"RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));
result &= TestNuGetRef("//r nuget \"RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));
result &= TestNuGetRef("//r nuget \"RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));
result &= TestNuGetRef("//r nuget \"RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));

Console.WriteLine(result);