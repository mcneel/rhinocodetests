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

bool res = true;

// with #r ================================================================================================
// using ,
res &= TestNuGetRef("#r \"nuget: RestSharp,110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
res &= TestNuGetRef("# r \"nuget: RestSharp , 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using ==
res &= TestNuGetRef("#r \"nuget: RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
res &= TestNuGetRef("# r \"nuget: RestSharp == 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using >=
res &= TestNuGetRef("#r \"nuget: RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));
res &= TestNuGetRef("# r \"nuget: RestSharp >= 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));

// using <=
res &= TestNuGetRef("#r \"nuget: RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));
res &= TestNuGetRef("# r \"nuget: RestSharp <= 110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));

// using >
res &= TestNuGetRef("#r \"nuget: RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));
res &= TestNuGetRef("# r \"nuget: RestSharp > 110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));

// using <
res &= TestNuGetRef("#r \"nuget: RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));
res &= TestNuGetRef("# r \"nuget: RestSharp < 110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));

// no version
res &= TestNuGetRef("#r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));
res &= TestNuGetRef("# r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));

// with //r ===============================================================================================
// using ,
res &= TestNuGetRef("//r \"nuget: RestSharp,110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
res &= TestNuGetRef("// r \"nuget: RestSharp , 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using ==
res &= TestNuGetRef("//r \"nuget: RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
res &= TestNuGetRef("// r \"nuget: RestSharp == 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));

// using >=
res &= TestNuGetRef("//r \"nuget: RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));
res &= TestNuGetRef("// r \"nuget: RestSharp >= 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));

// using <=
res &= TestNuGetRef("//r \"nuget: RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));
res &= TestNuGetRef("// r \"nuget: RestSharp <= 110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));

// using >
res &= TestNuGetRef("//r \"nuget: RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));
res &= TestNuGetRef("// r \"nuget: RestSharp > 110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));

// using <
res &= TestNuGetRef("//r \"nuget: RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));
res &= TestNuGetRef("// r \"nuget: RestSharp < 110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));

// no version
res &= TestNuGetRef("//r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));
res &= TestNuGetRef("// r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp"));


// legacy format ==========================================================================================
res &= TestNuGetRef("//r nuget \"RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0"));
res &= TestNuGetRef("//r nuget \"RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0"));
res &= TestNuGetRef("//r nuget \"RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0"));
res &= TestNuGetRef("//r nuget \"RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0"));
res &= TestNuGetRef("//r nuget \"RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0"));

// Console.WriteLine(res);
result = res;