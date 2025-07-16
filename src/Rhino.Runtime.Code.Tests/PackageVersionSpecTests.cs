using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code.Environments;
using Rhino.Runtime.Code.Platform;

namespace Rhino.Runtime.Code.Tests
{
  [TestFixture]
  public class PackageVersionSpecTests
  {
#if RC9_0
    [Test]
    public void TestPackageVersionSpec_Create_And_ToString()
    {
      var testCases = new List<(PackageVersionSpec spec, string expected)>
      {
          (new PackageVersionSpec(1),                      "1.*"),
          (new PackageVersionSpec(1, 2),                   "1.2.*"),
          (new PackageVersionSpec(1, 2, 3),                "1.2.3+*"),
          (new PackageVersionSpec(1, 2, 3, "beta"),        "1.2.3-beta+*"),
          (new PackageVersionSpec(1, 2, 3, "beta", 4),     "1.2.3-beta+4"),
          (new PackageVersionSpec(1, 2, 3, "", 4),         "1.2.3+4"),
          (new PackageVersionSpec("1.*"),                  "1.*"),
          (new PackageVersionSpec("1"),                    "1.*"),
          (new PackageVersionSpec("1.2.*"),                "1.2.*"),
          (new PackageVersionSpec("1.2"),                  "1.2.*"),
          (new PackageVersionSpec("1.2.3.*"),              "1.2.3+*"),
          (new PackageVersionSpec("1.2.3+*"),              "1.2.3+*"),
          (new PackageVersionSpec("1.2.3-*"),              "1.2.3-*"),
          (new PackageVersionSpec("1.2.3"),                "1.2.3+*"),
          (new PackageVersionSpec("1.2.3+4"),              "1.2.3+4"),
          (new PackageVersionSpec("1.2.3.4"),              "1.2.3+4"),
          (new PackageVersionSpec("1.2.3-beta+4"),         "1.2.3-beta+4"),
          (new PackageVersionSpec("1.2.3-beta+*"),         "1.2.3-beta+*"),
          (new PackageVersionSpec("1.2.3-beta+*"),         "1.2.3-beta+*"),
          (new PackageVersionSpec("1.2.3--beta-+*"),       "1.2.3-beta+*"),
          (new PackageVersionSpec("1.2.3-beta-2+*"),       "1.2.3-beta-2+*"),
          (new PackageVersionSpec("1.0rc1+abc123"),        "1.0.*"),
      };

      foreach ((PackageVersionSpec spec, string expected) in testCases)
      {
        Assert.That(spec.ToString(), Is.EqualTo(expected));
      }

      Assert.Throws<ArgumentException>(() => new PackageVersionSpec(""));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec("*.2"));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec("1.*.2"));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec("1.2.3-*+4"));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec("1.2."));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec(".1.2"));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec("latest"));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec(1, -1, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec(1, 2, -1, "", 4));
      Assert.Throws<ArgumentException>(() => new PackageVersionSpec(1, 2, -1, "*", 4));

      // we specifically allow this
      Assert.DoesNotThrow(() => new PackageVersionSpec("1.2-beta"));
      Assert.DoesNotThrow(() => new PackageVersionSpec("1.2.*-beta"));
    }

    [Test]
    public void TestPackageVersionSpec_Create_And_ToString_HostVersionSpec()
    {
      var testCases = new List<(HostVersionSpec spec, string expected)>
      {
          (new HostVersionSpec(1),                      "1.*"),
          (new HostVersionSpec(1, 2),                   "1.2.*"),
          (new HostVersionSpec(1, 2, 3),                "1.2.3+*"),
          (new HostVersionSpec(1, 2, 3, "beta"),        "1.2.3-beta+*"),
          (new HostVersionSpec(1, 2, 3, "beta", 4),     "1.2.3-beta+4"),
          (new HostVersionSpec(1, 2, 3, "", 4),         "1.2.3+4"),
          (new HostVersionSpec("1.*"),                  "1.*"),
          (new HostVersionSpec("1"),                    "1.*"),
          (new HostVersionSpec("1.2.*"),                "1.2.*"),
          (new HostVersionSpec("1.2"),                  "1.2.*"),
          (new HostVersionSpec("1.2.3.*"),              "1.2.3+*"),
          (new HostVersionSpec("1.2.3+*"),              "1.2.3+*"),
          (new HostVersionSpec("1.2.3-*"),              "1.2.3-*"),
          (new HostVersionSpec("1.2.3"),                "1.2.3+*"),
          (new HostVersionSpec("1.2.3+4"),              "1.2.3+4"),
          (new HostVersionSpec("1.2.3.4"),              "1.2.3+4"),
          (new HostVersionSpec("1.2.3-beta+4"),         "1.2.3-beta+4"),
          (new HostVersionSpec("1.2.3-beta+*"),         "1.2.3-beta+*"),
          (new HostVersionSpec("1.2.3-beta+*"),         "1.2.3-beta+*"),
          (new HostVersionSpec("1.2.3--beta-+*"),       "1.2.3-beta+*"),
          (new HostVersionSpec("1.2.3-beta-2+*"),       "1.2.3-beta-2+*"),
          (new HostVersionSpec("1.0rc1+abc123"),        "1.0.*"),
      };

      foreach ((HostVersionSpec spec, string expected) in testCases)
      {
        Assert.That(spec.ToString(), Is.EqualTo(expected));
      }

      Assert.Throws<ArgumentException>(() => new HostVersionSpec(""));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec("*.2"));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec("1.*.2"));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec("1.2.3-*+4"));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec("1.2."));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec(".1.2"));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec("latest"));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec(1, -1, 3, "", 4));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec(1, 2, -1, "", 4));
      Assert.Throws<ArgumentException>(() => new HostVersionSpec(1, 2, -1, "*", 4));

      // we specifically allow this
      Assert.DoesNotThrow(() => new HostVersionSpec("1.2-beta"));
      Assert.DoesNotThrow(() => new HostVersionSpec("1.2.*-beta"));
    }

    [Test]
    public void TestPackageVersionSpec_Match()
    {
      Assert.That(new PackageVersionSpec("1.2").Matches(new PackageVersionSpec("1.2"), PackageSpec.VersionCompareRule.Exact));
      Assert.That(!new PackageVersionSpec("1.2").Matches(new PackageVersionSpec("1.3"), PackageSpec.VersionCompareRule.Exact));
      Assert.That(new PackageVersionSpec("1.2").Matches(new PackageVersionSpec("1.3"), PackageSpec.VersionCompareRule.NewerThan));
      Assert.That(new PackageVersionSpec("1.2").Matches(new PackageVersionSpec("1.3"), PackageSpec.VersionCompareRule.NewerThanOrEqual));
    }
#endif
  }
}