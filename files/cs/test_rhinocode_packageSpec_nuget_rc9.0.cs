using System;
using System.Linq;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;


var pkgSpec = new PackageSpecifierByText();

bool TestNuGetRef(string refSpec, NuGetPackageSpec nugetSpec)
{
    var pkgs = pkgSpec.Specify(new TemplateScript("T", refSpec));
    var refNugetSpec = pkgs.Entries.First().Directive.SpecSet.First();
    return refNugetSpec.Equals(nugetSpec);
}

bool res = true;

// with #r ================================================================================================
// using ,
NuGetPackageSpec ns;
NuGetPackageSpec.TryParse("RestSharp==110.2.0", out ns);
res &= TestNuGetRef("#r \"nuget: RestSharp,110.2.0\"", ns);
res &= TestNuGetRef("# r \"nuget: RestSharp , 110.2.0\"", ns);

// using ==
res &= TestNuGetRef("#r \"nuget: RestSharp==110.2.0\"", ns);
res &= TestNuGetRef("# r \"nuget: RestSharp == 110.2.0\"", ns);

// using >=
NuGetPackageSpec.TryParse("RestSharp>=110.2.0", out ns);
res &= TestNuGetRef("#r \"nuget: RestSharp>=110.2.0\"", ns);
res &= TestNuGetRef("# r \"nuget: RestSharp >= 110.2.0\"", ns);

// using <=
NuGetPackageSpec.TryParse("RestSharp<=110.2.0", out ns);
res &= TestNuGetRef("#r \"nuget: RestSharp<=110.2.0\"", ns);
res &= TestNuGetRef("# r \"nuget: RestSharp <= 110.2.0\"", ns);

// using >
NuGetPackageSpec.TryParse("RestSharp>110.2.0", out ns);
res &= TestNuGetRef("#r \"nuget: RestSharp>110.2.0\"", ns);
res &= TestNuGetRef("# r \"nuget: RestSharp > 110.2.0\"", ns);

// using <
NuGetPackageSpec.TryParse("RestSharp<110.2.0", out ns);
res &= TestNuGetRef("#r \"nuget: RestSharp<110.2.0\"", ns);
res &= TestNuGetRef("# r \"nuget: RestSharp < 110.2.0\"", ns);

// no version
NuGetPackageSpec.TryParse("RestSharp", out ns);
res &= TestNuGetRef("#r \"nuget: RestSharp\"", ns);
res &= TestNuGetRef("# r \"nuget: RestSharp\"", ns);

// with //r ===============================================================================================
// using ,
NuGetPackageSpec.TryParse("RestSharp==110.2.0", out ns);
res &= TestNuGetRef("//r \"nuget: RestSharp,110.2.0\"", ns);
res &= TestNuGetRef("// r \"nuget: RestSharp , 110.2.0\"",ns);

// using ==
res &= TestNuGetRef("//r \"nuget: RestSharp==110.2.0\"", ns);
res &= TestNuGetRef("// r \"nuget: RestSharp == 110.2.0\"", ns);

// using >=
NuGetPackageSpec.TryParse("RestSharp>=110.2.0", out ns);
res &= TestNuGetRef("//r \"nuget: RestSharp>=110.2.0\"", ns);
res &= TestNuGetRef("// r \"nuget: RestSharp >= 110.2.0\"", ns);

// using <=
NuGetPackageSpec.TryParse("RestSharp<=110.2.0", out ns);
res &= TestNuGetRef("//r \"nuget: RestSharp<=110.2.0\"", ns);
res &= TestNuGetRef("// r \"nuget: RestSharp <= 110.2.0\"", ns);

// using >
NuGetPackageSpec.TryParse("RestSharp>110.2.0", out ns);
res &= TestNuGetRef("//r \"nuget: RestSharp>110.2.0\"", ns);
res &= TestNuGetRef("// r \"nuget: RestSharp > 110.2.0\"", ns);

// using <
NuGetPackageSpec.TryParse("RestSharp<110.2.0", out ns);
res &= TestNuGetRef("//r \"nuget: RestSharp<110.2.0\"", ns);
res &= TestNuGetRef("// r \"nuget: RestSharp < 110.2.0\"", ns);

// no version
NuGetPackageSpec.TryParse("RestSharp", out ns);
res &= TestNuGetRef("//r \"nuget: RestSharp\"", ns);
res &= TestNuGetRef("// r \"nuget: RestSharp\"", ns);


// legacy format ==========================================================================================
NuGetPackageSpec.TryParse("RestSharp==110.2.0", out ns);
res &= TestNuGetRef("//r nuget \"RestSharp==110.2.0\"", ns);

NuGetPackageSpec.TryParse("RestSharp>=110.2.0", out ns);
res &= TestNuGetRef("//r nuget \"RestSharp>=110.2.0\"", ns);

NuGetPackageSpec.TryParse("RestSharp<=110.2.0", out ns);
res &= TestNuGetRef("//r nuget \"RestSharp<=110.2.0\"", ns);

NuGetPackageSpec.TryParse("RestSharp<110.2.0", out ns);
res &= TestNuGetRef("//r nuget \"RestSharp<110.2.0\"", ns);

NuGetPackageSpec.TryParse("RestSharp>110.2.0", out ns);
res &= TestNuGetRef("//r nuget \"RestSharp>110.2.0\"", ns);

// Console.WriteLine(res);
result = res;