using System;
using System.IO;
using System.Threading.Tasks;

using NUnit.Framework;

namespace RhinoCodePlatform.Rhino3D.Tests
{
  [TestFixture]
  public class Yak_Tests : ScriptFixture
  {
    [Test]
    public async Task TestYak_PackageCanBeFoundBasedOnKeyword()
    {
      Assert.True(TryGetTestFilesPath(out string fileDir));
      dynamic client = Yak_Tests_Utils.TestPackageCanBeFoundBasedOnKeyword(Path.Combine(fileDir, "yak"));

      dynamic[] packages = await client.Search("guid:72828843-1a76-491e-8347-ccf3ca107cb9");
      Assert.AreEqual(1, packages.Length);

      dynamic p = packages[0];
      Assert.AreEqual("DKUI", p.Name);
      Assert.AreEqual("0.1.26664+9281", p.Version);
    }
  }

  static class Yak_Tests_Utils
  {
    public static object TestPackageCanBeFoundBasedOnKeyword(string packageSource)
    {
      // https://mcneel.myjetbrains.com/youtrack/issue/RH-87682

      // matches GH_YakDownloadFormEto.vb
      var prodInfo = new Yak.ProductHeaderValue("grasshopper_package_restore");
      Yak.IPackageRepository source = Yak.PackageRepositoryFactory.Create(packageSource, prodInfo);

      return new Yak.YakClient(source);
    }
  }
}
