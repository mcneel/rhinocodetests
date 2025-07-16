using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Projects;
using Rhino.Runtime.Code.Platform;

namespace Rhino.Runtime.Code.Tests
{
  [TestFixture]
  public class PackageVersionTests
  {
#if RC9_0
    [Test]
    public void TestPackageVersion_Create_And_ToString()
    {
      var testCases = new List<(PackageVersion spec, string expected)>
      {
          (new PackageVersion(1),                      "1"),
          (new PackageVersion(1, 2),                   "1.2"),
          (new PackageVersion(1, 2, "beta"),           "1.2-beta"),
          (new PackageVersion(1, 2, 3),                "1.2.3"),
          (new PackageVersion(1, 2, 3, "beta"),        "1.2.3-beta"),
          (new PackageVersion(1, 2, 3, "beta", 4),     "1.2.3-beta+4"),
          (new PackageVersion("1.*"),                  "1"),
          (new PackageVersion("1"),                    "1"),
          (new PackageVersion("1.2.*"),                "1.2"),
          (new PackageVersion("1.2"),                  "1.2"),
          (new PackageVersion("1.2.3.*"),              "1.2.3"),
          (new PackageVersion("1.2.3+*"),              "1.2.3"),
          (new PackageVersion("1.2.3-*"),              "1.2.3"),
          (new PackageVersion("1.2.3"),                "1.2.3"),
          (new PackageVersion("1.2.0+0"),              "1.2.0"), // no reason to list +0
          (new PackageVersion("1.2.3+0"),              "1.2.3"), // no reason to list +0
          (new PackageVersion("1.2.3+4"),              "1.2.3+4"),
          (new PackageVersion("1.2.3.4"),              "1.2.3+4"),
          (new PackageVersion("1.2.3-beta+4"),         "1.2.3-beta+4"),
          (new PackageVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new PackageVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new PackageVersion("1.2.3--beta-+*"),       "1.2.3-beta"),
          (new PackageVersion("1.2.3-beta-2+*"),       "1.2.3-beta-2"),
          (new PackageVersion("1.0rc1+abc123"),        "1.0"),
      };

      foreach ((PackageVersion spec, string expected) in testCases)
      {
        Assert.That(spec.ToString(), Is.EqualTo(expected));
      }

      Assert.Throws<ArgumentException>(() => new PackageVersion(""));
      Assert.Throws<ArgumentException>(() => new PackageVersion("*.2"));
      Assert.Throws<ArgumentException>(() => new PackageVersion("1.*.2"));
      Assert.Throws<ArgumentException>(() => new PackageVersion("1.2.3-*+4"));
      Assert.Throws<ArgumentException>(() => new PackageVersion("1.2."));
      Assert.Throws<ArgumentException>(() => new PackageVersion(".1.2"));
      Assert.Throws<ArgumentException>(() => new PackageVersion("latest"));
      Assert.Throws<ArgumentException>(() => new PackageVersion(-1));
      Assert.Throws<ArgumentException>(() => new PackageVersion(1, 2, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new PackageVersion(1, -1, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new PackageVersion(1, 2, -1, "", 4));
      Assert.Throws<ArgumentException>(() => new PackageVersion(1, 2, -1, "*", 4));

      // we specifically allow this
      Assert.DoesNotThrow(() => new PackageVersion("1.2-beta"));
      Assert.DoesNotThrow(() => new PackageVersion("1.2.*-beta"));
    }

    [Test]
    public void TestPackageVersion_Create_And_ToString_NuGetVersion()
    {
      var testCases = new List<(NuGetPackageVersion spec, string expected)>
      {
          (new NuGetPackageVersion(1, 2),                   "1.2"),
          (new NuGetPackageVersion(1, 2, "beta"),           "1.2-beta"),
          (new NuGetPackageVersion(1, 2, 3),                "1.2.3"),
          (new NuGetPackageVersion(1, 2, 3, "beta"),        "1.2.3-beta"),
          (new NuGetPackageVersion(1, 2, 3, "beta", 4),     "1.2.3-beta+4"),
          (new NuGetPackageVersion("1.2.*"),                "1.2"),
          (new NuGetPackageVersion("1.2"),                  "1.2"),
          (new NuGetPackageVersion("1.2.3.*"),              "1.2.3"),
          (new NuGetPackageVersion("1.2.3+*"),              "1.2.3"),
          (new NuGetPackageVersion("1.2.3-*"),              "1.2.3"),
          (new NuGetPackageVersion("1.2.3"),                "1.2.3"),
          (new NuGetPackageVersion("1.2.0+0"),              "1.2.0"), // no reason to list +0
          (new NuGetPackageVersion("1.2.3+0"),              "1.2.3"), // no reason to list +0
          (new NuGetPackageVersion("1.2.3+4"),              "1.2.3+4"),
          (new NuGetPackageVersion("1.2.3.4"),              "1.2.3+4"),
          (new NuGetPackageVersion("1.2.3-beta+4"),         "1.2.3-beta+4"),
          (new NuGetPackageVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new NuGetPackageVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new NuGetPackageVersion("1.2.3--beta-+*"),       "1.2.3-beta"),
          (new NuGetPackageVersion("1.2.3-beta-2+*"),       "1.2.3-beta-2"),
      };

      foreach ((NuGetPackageVersion spec, string expected) in testCases)
      {
        Assert.That(spec.ToString(), Is.EqualTo(expected));
      }

      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion(""));
      Assert.Throws<ArgumentOutOfRangeException>(() => new NuGetPackageVersion("1"));
      Assert.Throws<ArgumentOutOfRangeException>(() => new NuGetPackageVersion("1.*"));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion("*.2"));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion("1.*.2"));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion("1.2.3-*+4"));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion("1.2."));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion(".1.2"));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion("latest"));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion(1, 2, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion(1, -1, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion(1, 2, -1, "", 4));
      Assert.Throws<ArgumentException>(() => new NuGetPackageVersion(1, 2, -1, "*", 4));

      // we specifically allow this
      Assert.DoesNotThrow(() => new NuGetPackageVersion("1.2-beta"));
      Assert.DoesNotThrow(() => new NuGetPackageVersion("1.2.*-beta"));
    }

    [Test]
    public void TestPackageVersion_Create_And_ToString_ProjectVersion()
    {
      var testCases = new List<(ProjectVersion spec, string expected)>
      {
          (new ProjectVersion(1, 2),                   "1.2"),
          (new ProjectVersion(1, 2, "beta"),           "1.2-beta"),
          (new ProjectVersion(1, 2, 3),                "1.2.3"),
          (new ProjectVersion(1, 2, 3, "beta"),        "1.2.3-beta"),
          (new ProjectVersion(1, 2, 3, "beta", 4),     "1.2.3-beta+4"),
          (new ProjectVersion("1.*"),                  "1"),
          (new ProjectVersion("1"),                    "1"),
          (new ProjectVersion("1.2.*"),                "1.2"),
          (new ProjectVersion("1.2"),                  "1.2"),
          (new ProjectVersion("1.2.3.*"),              "1.2.3"),
          (new ProjectVersion("1.2.3+*"),              "1.2.3"),
          (new ProjectVersion("1.2.3-*"),              "1.2.3"),
          (new ProjectVersion("1.2.3"),                "1.2.3"),
          (new ProjectVersion("1.2.0+0"),              "1.2.0"), // no reason to list +0
          (new ProjectVersion("1.2.3+0"),              "1.2.3"), // no reason to list +0
          (new ProjectVersion("1.2.3+4"),              "1.2.3+4"),
          (new ProjectVersion("1.2.3.4"),              "1.2.3+4"),
          (new ProjectVersion("1.2.3-beta+4"),         "1.2.3-beta+4"),
          (new ProjectVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new ProjectVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new ProjectVersion("1.2.3--beta-+*"),       "1.2.3-beta"),
          (new ProjectVersion("1.2.3-beta-2+*"),       "1.2.3-beta-2"),
          (new ProjectVersion("1.0rc1+abc123"),        "1.0"),
      };

      foreach ((ProjectVersion spec, string expected) in testCases)
      {
        Assert.That(spec.ToString(), Is.EqualTo(expected));
      }

      Assert.Throws<ArgumentException>(() => new ProjectVersion(""));
      Assert.Throws<ArgumentException>(() => new ProjectVersion("*.2"));
      Assert.Throws<ArgumentException>(() => new ProjectVersion("1.*.2"));
      Assert.Throws<ArgumentException>(() => new ProjectVersion("1.2.3-*+4"));
      Assert.Throws<ArgumentException>(() => new ProjectVersion("1.2."));
      Assert.Throws<ArgumentException>(() => new ProjectVersion(".1.2"));
      Assert.Throws<ArgumentException>(() => new ProjectVersion("latest"));
      Assert.Throws<ArgumentException>(() => new ProjectVersion(1, 2, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new ProjectVersion(1, -1, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new ProjectVersion(1, 2, -1, "", 4));
      Assert.Throws<ArgumentException>(() => new ProjectVersion(1, 2, -1, "*", 4));

      // we specifically allow this
      Assert.DoesNotThrow(() => new ProjectVersion("1.2-beta"));
      Assert.DoesNotThrow(() => new ProjectVersion("1.2.*-beta"));
    }

    [Test]
    public void TestPackageVersion_Create_And_ToString_HostVersion()
    {
      var testCases = new List<(HostVersion spec, string expected)>
      {
          (new HostVersion(1),                      "1"),
          (new HostVersion(1, 2),                   "1.2"),
          (new HostVersion(1, 2, "beta"),           "1.2-beta"),
          (new HostVersion(1, 2, 3),                "1.2.3"),
          (new HostVersion(1, 2, 3, "beta"),        "1.2.3-beta"),
          (new HostVersion(1, 2, 3, "beta", 4),     "1.2.3-beta+4"),
          (new HostVersion("1.*"),                  "1"),
          (new HostVersion("1"),                    "1"),
          (new HostVersion("1.2.*"),                "1.2"),
          (new HostVersion("1.2"),                  "1.2"),
          (new HostVersion("1.2.3.*"),              "1.2.3"),
          (new HostVersion("1.2.3+*"),              "1.2.3"),
          (new HostVersion("1.2.3-*"),              "1.2.3"),
          (new HostVersion("1.2.3"),                "1.2.3"),
          (new HostVersion("1.2.0+0"),              "1.2.0"), // no reason to list +0
          (new HostVersion("1.2.3+0"),              "1.2.3"), // no reason to list +0
          (new HostVersion("1.2.3+4"),              "1.2.3+4"),
          (new HostVersion("1.2.3.4"),              "1.2.3+4"),
          (new HostVersion("1.2.3-beta+4"),         "1.2.3-beta+4"),
          (new HostVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new HostVersion("1.2.3-beta+*"),         "1.2.3-beta"),
          (new HostVersion("1.2.3--beta-+*"),       "1.2.3-beta"),
          (new HostVersion("1.2.3-beta-2+*"),       "1.2.3-beta-2"),
          (new HostVersion("1.0rc1+abc123"),        "1.0"),
      };

      foreach ((HostVersion spec, string expected) in testCases)
      {
        Assert.That(spec.ToString(), Is.EqualTo(expected));
      }

      Assert.Throws<ArgumentException>(() => new HostVersion(""));
      Assert.Throws<ArgumentException>(() => new HostVersion("*.2"));
      Assert.Throws<ArgumentException>(() => new HostVersion("1.*.2"));
      Assert.Throws<ArgumentException>(() => new HostVersion("1.2.3-*+4"));
      Assert.Throws<ArgumentException>(() => new HostVersion("1.2."));
      Assert.Throws<ArgumentException>(() => new HostVersion(".1.2"));
      Assert.Throws<ArgumentException>(() => new HostVersion("latest"));
      Assert.Throws<ArgumentException>(() => new HostVersion(-1));
      Assert.Throws<ArgumentException>(() => new HostVersion(1, 2, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new HostVersion(1, -1, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new HostVersion(1, 2, -1, "", 4));
      Assert.Throws<ArgumentException>(() => new HostVersion(1, 2, -1, "*", 4));

      // we specifically allow this
      Assert.DoesNotThrow(() => new HostVersion("1.2-beta"));
      Assert.DoesNotThrow(() => new HostVersion("1.2.*-beta"));
    }
#endif
  }
}