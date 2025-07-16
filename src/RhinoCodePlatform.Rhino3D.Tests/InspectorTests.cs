using System;
using System.Linq;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

using Rhino.Runtime.Code.Inspection;

namespace RhinoCodePlatform.Rhino3D.Tests
{
  [TestFixture]
  public class InspectorTests : ScriptFixture
  {
    protected class OverridePackageSpecInspector : Inspector
    {
      public bool Enabled { get; set; } = true;

      public OverridePackageSpecInspector() : base(new InspectorIdentity("mcneel.overrideSpec.testInspector")) { }

      public override bool InspectBefore(Code code, RunContext context) => true;
      public override void InspectException(Code code, RunContext context, Exception ex) { }
      public override void InspectAfter(Code code, RunContext context) { }

      public override bool InspectPackage(PackageSpec spec, out PackageSpec overrideSpec)
      {
        overrideSpec = Enabled ? new PackageSpec("NPOI==2.7.2") : spec;
        return true;
      }
    }

    [Test]
    public void TestInspector_Override_PackageSpec()
    {
      var inspector = new OverridePackageSpecInspector();
      Inspector.Register(inspector);

      ILanguage csharp = GetLanguage(LanguageSpec.CSharp);
      Code code = csharp.CreateCode(@"
# r ""nuget: NPOI, 2.6.2""
");

      code.RestorePackages();
      CompilePackageSet c = code.QueryPackages();

      var v = new PackageVersion("2.7.2");
      Assert.IsInstanceOf<NuGetPackage>(c.FirstOrDefault(p => p.Id == "NPOI" && v.Matches(p.Version, PackageSpec.VersionCompareRule.Exact)));
      Assert.DoesNotThrow(() => code.Run());

      inspector.Enabled = false;
    }
  }
}