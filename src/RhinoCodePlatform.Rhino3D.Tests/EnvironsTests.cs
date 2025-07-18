using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;
using NuGet.Versioning;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Languages.PythonNet;

using RhinoCodePlatform.Rhino3D.Testing;

namespace RhinoCodePlatform.Rhino3D.Tests
{
  [TestFixture]
  public class Environs_Tests : ScriptFixture
  {
    protected static bool GetPackageVersions(PackageSpec spec, int count)
    {
      IEnumerable<NuGetVersion> versions = NuGetEnvirons.User.GetPackageVersions(spec);
      return versions.Count() == count;
    }

    [Test]
    public void TestEnvirons_Language_Versions()
    {
      // this used to be test_rhinocode_registry_version.cs test file
      IOrderedEnumerable<Rhino.Runtime.Code.Registry.RegistryVersionedSpec.SpecVersion> versions
        = RhinoCode.Languages
                   .Select(l => l.Id.Version)
                   .OrderByDescending(v => v.ToVersion());

      // csharp 9
      Assert.That(versions.First().Major == 9);

      // markdown 0.30
      Assert.That(versions.Last().Major == 0);
    }

    [Test]
    public void TestEnvirons_NuGet_Versions()
    {
      // this used to be test_rhinocode_nuget_getPackageVersion.cs test file
      Assert.That(GetPackageVersions(new PackageSpec("RhinoCommon==8.5"), 1));
      Assert.That(GetPackageVersions(new PackageSpec("RhinoCommon==8.5-rc"), 6));
      Assert.That(GetPackageVersions(new PackageSpec("RhinoCommon==8.5-wip"), 0));
    }

#if RC9_0
    [Test]
    public void TestEnvirons_SpecEntryCache()
    {
      Code code = GetLanguage(LanguageSpec.Any).CreateCode(@"
#r ""nuget: RhinoCommon, 8.21.25188.17001""
      ");

      var rpw = new RestoreProgressWatcher();
      code.RestorePackages(rpw);
      Assert.IsTrue(rpw.HasReports);

      code.Text.Set(@"
#r ""nuget: RhinoCommon, 8.21.25188.17001""



      ");

      rpw.Reset();
      code.RestorePackages(rpw);
      Assert.IsFalse(rpw.HasReports);

      code.Text.Set(@"
#r ""nuget: RhinoCommon, 8.21.25188.17001""
#r ""nuget: Grasshopper, 8.21.25188.17001""



      ");

      rpw.Reset();
      code.RestorePackages(rpw);
      Assert.IsTrue(rpw.HasReports);
    }

    [Test]
    public void TestEnvirons_Reset_Environ_On_EnvironSpec_Remove()
    {
      ILanguage py3 = GetLanguage(LanguageSpec.Python3);
      Code code = py3.CreateCode(@"
# venv: test-environspec-remove
      ");

      code.RestoreEnviron();
      Assert.AreEqual(py3.Environs.OfIdentity("test-environspec-remove"), code.Environ);

      code.Text.Set(string.Empty);
      code.RestoreEnviron();
      Assert.AreEqual(py3.Environs.Shared, code.Environ);
    }

    [Test]
    public void TestEnvirons_NuGet_Specs()
    {
      // this is RC9.0 version of test_rhinocode_packageSpec_nuget.cs
      var pkgSpec = new PackageSpecifierByText();

      bool TestNuGetRef(string refSpec, NuGetPackageSpec nugetSpec)
      {
        PackageSpecifierResult pkgs = pkgSpec.Specify(new TemplateScript("T", refSpec));
        PackageSpec refNugetSpec = pkgs.Entries.First().Directive.SpecSet.First();
        return refNugetSpec.Equals(nugetSpec);
      }

      // with #r ================================================================================================
      // using ,
      Assert.That(TestNuGetRef("#r \"nuget: RestSharp,110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));
      Assert.That(TestNuGetRef("# r \"nuget: RestSharp , 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));

      // using ==
      Assert.That(TestNuGetRef("#r \"nuget: RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0")));
      Assert.That(TestNuGetRef("# r \"nuget: RestSharp == 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0")));

      // using >=
      Assert.That(TestNuGetRef("#r \"nuget: RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));
      Assert.That(TestNuGetRef("# r \"nuget: RestSharp >= 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));

      // using <=
      Assert.That(TestNuGetRef("#r \"nuget: RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0")));
      Assert.That(TestNuGetRef("# r \"nuget: RestSharp <= 110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0")));

      // using >
      Assert.That(TestNuGetRef("#r \"nuget: RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0")));
      Assert.That(TestNuGetRef("# r \"nuget: RestSharp > 110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0")));

      // using <
      Assert.That(TestNuGetRef("#r \"nuget: RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0")));
      Assert.That(TestNuGetRef("# r \"nuget: RestSharp < 110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0")));

      // no version
      Assert.That(TestNuGetRef("#r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp")));
      Assert.That(TestNuGetRef("# r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp")));

      // with //r ===============================================================================================
      // using ,
      Assert.That(TestNuGetRef("//r \"nuget: RestSharp,110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));
      Assert.That(TestNuGetRef("// r \"nuget: RestSharp , 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));

      // using ==
      Assert.That(TestNuGetRef("//r \"nuget: RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0")));
      Assert.That(TestNuGetRef("// r \"nuget: RestSharp == 110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0")));

      // using >=
      Assert.That(TestNuGetRef("//r \"nuget: RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));
      Assert.That(TestNuGetRef("// r \"nuget: RestSharp >= 110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));

      // using <=
      Assert.That(TestNuGetRef("//r \"nuget: RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0")));
      Assert.That(TestNuGetRef("// r \"nuget: RestSharp <= 110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0")));

      // using >
      Assert.That(TestNuGetRef("//r \"nuget: RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0")));
      Assert.That(TestNuGetRef("// r \"nuget: RestSharp > 110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0")));

      // using <
      Assert.That(TestNuGetRef("//r \"nuget: RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0")));
      Assert.That(TestNuGetRef("// r \"nuget: RestSharp < 110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0")));

      // no version
      Assert.That(TestNuGetRef("//r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp")));
      Assert.That(TestNuGetRef("// r \"nuget: RestSharp\"", new NuGetPackageSpec("RestSharp")));


      // legacy format ==========================================================================================
      Assert.That(TestNuGetRef("//r nuget \"RestSharp==110.2.0\"", new NuGetPackageSpec("RestSharp==110.2.0")));
      Assert.That(TestNuGetRef("//r nuget \"RestSharp>=110.2.0\"", new NuGetPackageSpec("RestSharp>=110.2.0")));
      Assert.That(TestNuGetRef("//r nuget \"RestSharp<=110.2.0\"", new NuGetPackageSpec("RestSharp<=110.2.0")));
      Assert.That(TestNuGetRef("//r nuget \"RestSharp<110.2.0\"", new NuGetPackageSpec("RestSharp<110.2.0")));
      Assert.That(TestNuGetRef("//r nuget \"RestSharp>110.2.0\"", new NuGetPackageSpec("RestSharp>110.2.0")));
    }

    [Test]
    public void TestEnvirons_NuGet_Installed()
    {
      NuGetEnvironConnection cxn = NuGetEnvirons.User.Connect(new ConnectionContext());
      IPackageInfo pi = cxn.QueryLocal(CancellationToken.None).FirstOrDefault();
      Assert.NotNull(pi);

      Assert.AreEqual(PackageAvailability.Available, pi.Availability);
    }


    [Test]
    public void TestEnvirons_NuGet_Installed_Rhino_DOT_Scripting()
    {
      NuGetEnvirons.User.AddPackage(new PackageSpec("Rhino.Scripting"));
      NuGetEnvironConnection cxn = NuGetEnvirons.User.Connect(new ConnectionContext());

      IPackageInfo pi = cxn.QueryLocal(new PackageSpec[] { new("RhinoScript") }, CancellationToken.None).FirstOrDefault();
      Assert.NotNull(pi);
      Assert.That(pi.Id, Is.EqualTo("Rhino.Scripting"));
      Assert.That(pi.Availability, Is.EqualTo(PackageAvailability.Available));

      IPackageInfo rpi = cxn.Query(new PackageSpec[] { new("RhinoScript") }, new EnvironQueryOptions(), CancellationToken.None).FirstOrDefault();
      Assert.NotNull(rpi);
      Assert.That(rpi.Id, Is.EqualTo("Rhino3dm"));
      Assert.That(rpi.Availability, Is.EqualTo(PackageAvailability.AvailableOnProvider));
    }

    [Test]
    public void TestEnvirons_LibFile_Installed()
    {
      LibraryFileEnvironConnection cxn = LibraryFileEnvirons.User.Connect(new ConnectionContext());
      IEnumerable<IPackageInfo> pinfos = cxn.QueryLocal(CancellationToken.None);
      Assert.IsTrue(pinfos.All(pi => pi.Availability == PackageAvailability.AvailableOnHost || pi.Availability == PackageAvailability.AvailableOnPlatform));
    }

    [Test]
    public void TestEnvirons_GH2_Plugins()
    {
      var spec = new PackageSpec("G2");
      LibraryFileEnvironConnection cxn = LibraryFileEnvirons.User.Connect(new ConnectionContext());
      HashSet<IPackageInfo> pinfos = cxn.QueryLocal(new PackageSpec[] { spec }, CancellationToken.None).ToHashSet();

      IPackageInfo pi = pinfos.FirstOrDefault(p => p.Id.StartsWith("G2ScriptComponents"));
      Assert.NotNull(pi);

      Assert.IsTrue(pi.Description.StartsWith("Mixed Grasshopper v2 & Rhino Plugin"));
    }

    [Test]
    public void TestEnvirons_Parse_And_Diagnostics()
    {
      Code code = GetLanguage(LanguageSpec.Python3).CreateCode(@"#! python 3

# shorthands
# r: pandas
# r: path/to/package.whl
# r: numpy git+https://github.com/huggingface/transformers.git

# full package specs
# r ""pip: pandas""
# r ""pip: git+https://github.com/huggingface/transformers.git@096f25ae1f501a084d8ff2dcaf25fbc2bd60eba4""
# r ""pip: package --index-url https://custom.pypi.org/simple""
# r ""pip: package --find-links https://example.com/packages""
# r ""pip: --editable /Users/ein/Downloads""
# r ""pip: --editable /somewhere/missing""
# r ""pip: --editable git+https://github.com/huggingface/transformers.git""
# r ""wheel: path/to/package.whl""
# r ""nuget: Newtonsoft.Json, 13.0.3""
    # r ""nuget: Newtonsoft.Json, 13.0.3""
# r ""yak: LunchBox, 2025.5.5.0""
# r ""yak: LunchBox, 2025.5.5+0, LunchBox.gha""
# r ""lib: /path/to/module.rhp""
# r ""lib: /path/to/module.gha""
# r ""lib: /path/to/module.so""
# r ""/path/to/module.dll""

# deprecated but works
# r nuget ""Newtonsoft.Json, 13.0.3""
      ");

      PackageSpecEntrySet entries = code.Text.QueryPackageSpecs().Entries;

      PackageSpecEntry entry;

      int index = 0;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("pandas").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      // Assert.Contains(CPythonPackageSpec.Parse("path/to/package.whl").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("git+https://github.com/huggingface/transformers.git").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("pandas").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("git+https://github.com/huggingface/transformers.git@096f25ae1f501a084d8ff2dcaf25fbc2bd60eba4").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("package --index-url https://custom.pypi.org/simple").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("package --find-links https://example.com/packages").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("--editable /Users/ein/Downloads").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("--editable /somewhere/missing").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(CPythonPackageSpec.Parse("--editable git+https://github.com/huggingface/transformers.git").First(), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new PackageSpec("Newtonsoft.Json", "13.0.3", PackageSpec.VersionCompareRule.NewerThanOrEqual), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new PackageSpec("Newtonsoft.Json>=13.0.3"), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new PackageSpec("LunchBox", "2025.5.5.0", PackageSpec.VersionCompareRule.NewerThanOrEqual), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new PackageSpec("LunchBox>=2025.5.5+0"), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new LibraryFileSpec("/path/to/module.rhp"), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new LibraryFileSpec("/path/to/module.gha"), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new LibraryFileSpec("/path/to/module.so"), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new LibraryFileSpec("/path/to/module.dll"), entry.Directive.SpecSet.ToArray());

      index++;
      entry = entries.ElementAt(index);
      Assert.AreEqual(1, entry.Directive.SpecSet.Count);
      Assert.Contains(new PackageSpec("Newtonsoft.Json>=13.0.3"), entry.Directive.SpecSet.ToArray());

      Diagnostic[] diags = code.Diagnose(new DiagnoseOptions()).ToArray();

      Diagnostic diag;

      diag = diags.First(d => d.Reference.Position.LineNumber == 16);
      Assert.AreEqual(DiagnosticSeverity.Error, diag.Severity);
      Assert.IsTrue(diag.Message.StartsWith("Specified package manager is not available"));

      diag = diags.First(d => d.Reference.Position.LineNumber == 27);
      Assert.AreEqual(DiagnosticSeverity.Error, diag.Severity);
      Assert.IsTrue(diag.Message.StartsWith("Legacy package specification. Change to '# r \"nuget: Newtonsoft.Json, 13.0.3\"'"));
    }

    [Test]
    public void TestEnvirons_PackageAvailability()
    {
      IPackageInfo p;
      IPackageInfo ps;
      var ctx = new ConnectionContext();
      var opts = new EnvironQueryOptions();
      CancellationToken token = CancellationToken.None;

      // PyPI
      ILanguage py3 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);
      using IEnvironConnection py3cn = py3.Environs.Connect(ctx);
      p = py3cn.QueryLocal(token).First();
      Assert.That(p.Availability, Is.EqualTo(PackageAvailability.Available));
      ps = py3cn.Query(new PackageSpec[] { new PackageSpec("numpy") }, opts, token).First();
      Assert.That(ps.Availability, Is.EqualTo(PackageAvailability.AvailableOnProvider));


      // NuGet
      ILanguage cs = RhinoCode.Languages.QueryLatest(LanguageSpec.CSharp);
      using IEnvironConnection cscn = cs.Environs.Connect(ctx);
      p = cscn.QueryLocal(token).First();
      Assert.That(p.Availability, Is.EqualTo(PackageAvailability.Available));
      ps = cscn.Query(new PackageSpec[] { new PackageSpec("RestSharp") }, opts, token).First();
      Assert.That(ps.Availability, Is.EqualTo(PackageAvailability.AvailableOnProvider));

      // Yak
      IEnvirons yak = RhinoCode.PackageEnvirons.WherePasses(new EnvironsSpec("*.*.yak")).First();
      yak.AddPackages(new PackageSpec[] { new("BitmapPlus"), new("BFS") });
      using IEnvironConnection yakcn = yak.Connect(ctx);
      p = yakcn.QueryLocal(token).First();
      Assert.That(p.Availability, Is.EqualTo(PackageAvailability.Available));
      ps = yakcn.Query(new PackageSpec[] { new("BitmapPlus") }, opts, token).First();
      Assert.That(ps.Availability, Is.EqualTo(PackageAvailability.AvailableOnProvider));

      // Wheel
      // var py2 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python2);

      // Lib
      IEnvirons lib = RhinoCode.PackageEnvirons.WherePasses(new EnvironsSpec("*.*.lib")).First();
      using IEnvironConnection libcn = lib.Connect(ctx);
      p = libcn.QueryLocal(new PackageSpec[] { new PackageSpec("__Python") }, token).First();
      Assert.That(p.Availability, Is.EqualTo(PackageAvailability.AvailableOnHost));
      p = libcn.QueryLocal(new PackageSpec[] { new PackageSpec("RhinoCommon") }, token).First();
      Assert.That(p.Availability, Is.EqualTo(PackageAvailability.AvailableOnPlatform));
    }

    [Test]
    public void TestEnvirons_Spec_To_Directive()
    {
      PackageSpecDirective[] directives;
      PackageSpecDirective d;

      // PyPI
      ILanguage py3 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);
      Assert.Throws<ArgumentException>(() => py3.Environs.GetDirectives("certifi>=1.2.*").ToArray());

      directives = py3.Environs.GetDirectives("jax[tpu]").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"pip: jax[tpu]\""));

      directives = py3.Environs.GetDirectives("jax[tpu]>=1.2.3").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"pip: jax[tpu]>=1.2.3\""));

      // NuGet
      ILanguage cs = RhinoCode.Languages.QueryLatest(LanguageSpec.CSharp);

      directives = cs.Environs.GetDirectives("RestSharp, 1.2").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"nuget: RestSharp, 1.2\""));

      directives = cs.Environs.GetDirectives("RestSharp, 1.2.*").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"nuget: RestSharp, 1.2\""));

      directives = cs.Environs.GetDirectives("RestSharp>=1.2.*").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"nuget: RestSharp, 1.2\""));

      // Yak
      IEnvirons yak = RhinoCode.PackageEnvirons.WherePasses(new EnvironsSpec("*.*.yak")).First();

      directives = yak.GetDirectives("LunchBox, 1.2").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"yak: LunchBox, 1.2\""));

      directives = yak.GetDirectives("LunchBox, 1.2.*").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"yak: LunchBox, 1.2\""));

      directives = yak.GetDirectives("LunchBox>=1.2.*").ToArray();
      d = directives[0];
      Assert.That(d.Text, Is.EqualTo("# r \"yak: LunchBox, 1.2\""));

      // Wheel
      ILanguage py2 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python2);
      Assert.Throws<ArgumentException>(() => py2.Environs.GetDirectives("RestSharp, 1.2").ToArray());
      Assert.Throws<ArgumentException>(() => py2.Environs.GetDirectives("RestSharp.dll").ToArray());
      Assert.Throws<ArgumentException>(() => py2.Environs.GetDirectives("RestSharp.whl").ToArray());
      Assert.DoesNotThrow(() => py2.Environs.GetDirectives("natsort-3.5.5-py2.py3-none-any.whl").ToArray());

      // Lib
      IEnvirons lib = RhinoCode.PackageEnvirons.WherePasses(new EnvironsSpec("*.*.lib")).First();
      Assert.DoesNotThrow(() => lib.GetDirectives("LunchBox, 1.2").ToArray());
    }

    [Test]
    public void TestEnvirons_Insert_Directive()
    {
      ILanguage py3 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);
      string script;
      Code code;

      // test inserting directives and matching line ending
      script = "#! python 3\n# loads cpython (fails ref)";
      code = py3.CreateCode(script);
      code.Text.Set(py3.Environs.GetDirectives("certifi>=1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}\n# r \"pip: certifi>=1.2.3\""));

      script = "#! python 3\n# loads cpython (fails ref)\n";
      code = py3.CreateCode(script);
      code.Text.Set(py3.Environs.GetDirectives("certifi>=1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}# r \"pip: certifi>=1.2.3\"\n"));

      script = "#! python 3\r\n# loads cpython (fails ref)";
      code = py3.CreateCode(script);
      code.Text.Set(py3.Environs.GetDirectives("certifi>=1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}\r\n# r \"pip: certifi>=1.2.3\""));

      script = "#! python 3\r\n# loads cpython (fails ref)\r\n";
      code = py3.CreateCode(script);
      code.Text.Set(py3.Environs.GetDirectives("certifi>=1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}# r \"pip: certifi>=1.2.3\"\r\n"));

      script = "#! python 3\n# loads cpython (fails ref)";
      code = py3.CreateCode(script + "\nm = 12");
      code.Text.Set(py3.Environs.GetDirectives("certifi>=1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}\n# r \"pip: certifi>=1.2.3\"\nm = 12"));
    }

    [Test]
    public void TestEnvirons_Insert_Directive_AfterSame()
    {
      ILanguage py3 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);
      string script;
      string expected;
      Code code;

      // test inserting directives and matching line ending
      const string P = "#";
      script = $@"#! python 3
{P} r ""pip: numpy>=2.3.1""
{P} r ""pip: scipy>=1.16.0""
{P} r ""pip: certifi>=2025.4.26""
{P} r ""nuget: System.Runtime.WindowsRuntime, 4.3.0+0""
{P} r ""yak: LunchBox, 2025.5.5""
{P} r ""lib: RhinoCodePlatform.Rhino3D.dll""
{P} r ""lib: GalapagosComponents.gha""
{P} r ""lib: Galapagos.dll""
{P} r ""lib: KangarooSolver.dll""
{P} r ""lib: Kangaroo2Component.gha""
";

      expected = $@"#! python 3
{P} r ""pip: numpy>=2.3.1""
{P} r ""pip: scipy>=1.16.0""
{P} r ""pip: certifi>=2025.4.26""
{P} r ""nuget: System.Runtime.WindowsRuntime, 4.3.0+0""
{P} r ""nuget: RestSharp, 112.1.0""
{P} r ""yak: LunchBox, 2025.5.5""
{P} r ""lib: RhinoCodePlatform.Rhino3D.dll""
{P} r ""lib: GalapagosComponents.gha""
{P} r ""lib: Galapagos.dll""
{P} r ""lib: KangarooSolver.dll""
{P} r ""lib: Kangaroo2Component.gha""
";

      code = py3.CreateCode(script);
      code.Text.Set(NuGetEnvirons.User.GetDirectives("RestSharp, 112.1.0"));
      Assert.That((string)code.Text, Is.EqualTo(expected));

      expected = $@"#! python 3
{P} r ""pip: numpy>=2.3.1""
{P} r ""pip: scipy>=1.16.0""
{P} r ""pip: certifi>=2025.4.26""
{P} r ""nuget: System.Runtime.WindowsRuntime, 4.3.0+0""
{P} r ""nuget: RestSharp, 112.1.0""
{P} r ""yak: LunchBox, 2025.5.5""
{P} r ""lib: RhinoCodePlatform.Rhino3D.dll""
{P} r ""lib: GalapagosComponents.gha""
{P} r ""lib: Galapagos.dll""
{P} r ""lib: KangarooSolver.dll""
{P} r ""lib: Kangaroo2Component.gha""
";

      code = py3.CreateCode(script);
      code.Text.Set(NuGetEnvirons.User.GetDirectives("RestSharp>=112.1.0"));
      Assert.That((string)code.Text, Is.EqualTo(expected));
    }

    [Test]
    public void TestEnvirons_Insert_Directive_AfterPEP723()
    {
      ILanguage py3 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);
      string script;
      Code code;

      // test inserting directives and matching line ending
      script = @"# /// script
# requires-python = "">=3""
# dependencies = [
#   ""requests<3"",
#   ""rich"",
# ]
# ///";

      code = py3.CreateCode(script);
      code.Text.Set(NuGetEnvirons.User.GetDirectives("RestSharp, 1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}\r\n# r \"nuget: RestSharp, 1.2.3\""));

      code = py3.CreateCode(script + "\r\n");
      code.Text.Set(NuGetEnvirons.User.GetDirectives("RestSharp, 1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}\r\n# r \"nuget: RestSharp, 1.2.3\"\r\n"));

      code = py3.CreateCode(script + "\r\n");
      code.Text.Set(NuGetEnvirons.User.GetDirectives("RestSharp, 1.2.3+0"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}\r\n# r \"nuget: RestSharp, 1.2.3\"\r\n"));
    }

    [Test]
    public void TestEnvirons_Insert_Directive_AfterLangSpec()
    {
      ILanguage py3 = RhinoCode.Languages.QueryLatest(LanguageSpec.Python3);
      string script;
      Code code;

      // test inserting directives and matching line ending
      script = "#! python 3";
      code = py3.CreateCode(script);
      code.Text.Set(py3.Environs.GetDirectives("certifi>=1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}\n# r \"pip: certifi>=1.2.3\""));

      script = "#! python 3\n";
      code = py3.CreateCode(script);
      code.Text.Set(py3.Environs.GetDirectives("certifi>=1.2.3"));
      Assert.That((string)code.Text, Is.EqualTo($"{script}# r \"pip: certifi>=1.2.3\"\n"));
    }

    static IEnumerable<TestCaseData> GetNuGetEnvironsTextToSpecBadSpecCases()
    {
      yield return new("RestSharp,");
      yield return new("RestSharp, 1.2.");
      yield return new("RestSharp, .1.2");
      yield return new("RestSharp>=");
    }

    [Test, TestCaseSource(nameof(GetNuGetEnvironsTextToSpecBadSpecCases))]
    public void TestEnvirons_NuGet_TextToSpec_BadSpec(string args)
    {
      Assert.Throws<ArgumentException>(() => NuGetEnvirons.User.GetDirectives(args).ToArray());
    }

    static IEnumerable<TestCaseData> GetYakEnvironsTextToSpecBadSpecCases()
    {
      yield return new("LunchBox,");
      yield return new("LunchBox, 1.2.");
      yield return new("LunchBox, .1.2");
      yield return new("LunchBox>=");
    }

    [Test, TestCaseSource(nameof(GetYakEnvironsTextToSpecBadSpecCases))]
    public void TestEnvirons_Yak_TextToSpec_BadSpec(string args)
    {
      IEnvirons yak = RhinoCode.PackageEnvirons.WherePasses(new EnvironsSpec("*.*.yak")).First();
      Assert.Throws<ArgumentException>(() => yak.GetDirectives(args).ToArray());
    }

    [Test]
    public void TestEnvirons_RestoreCode_And_Library()
    {
      ILanguage py3 = GetLanguage(LanguageSpec.Python3);
      Code code = py3.CreateCode("#r \"nuget: AWSSDK.Core, 4.0.0.16\"");

      ILanguage csharp = GetLanguage(LanguageSpec.CSharp);
      TryGetTestFilesPath(out string fileDir);
      ILanguageLibrary library = csharp.CreateLibrary(new Uri(Path.Combine(fileDir, "cs", "test_library_pkgrestore")));
      code.Libraries.Add(library);

      var rpw = new RestoreProgressWatcher();
      code.RestorePackages(rpw);

      Assert.That(rpw.Contains("Downloading AWSSDK.Core"));
      Assert.That(rpw.Contains("Downloading MathNet.Numerics"));
    }
#endif
  }
}

