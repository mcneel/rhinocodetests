using System;

using NUnit.Framework;

using Rhino.Runtime.Code.Environments;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class PackageSpecTests : Rhino.Testing.Fixtures.RhinoTestFixture
    {
#if RC8_8
        [Test]
        public void TestPythonPackageSpecNormalizedIdMatch()
        {
            var spec = new PythonPackageSpec("wood-nano", PackageVersionSpec.Any);

            PythonPackage package;

            package = new PythonPackage("wood_nano", new PackageVersion(0, 1, 1));
            Assert.IsTrue(spec.Matches(package));

            package = new PythonPackage("wood--nano", new PackageVersion(0, 1, 1));
            Assert.IsTrue(spec.Matches(package));

            package = new PythonPackage("wood__nano", new PackageVersion(0, 1, 1));
            Assert.IsTrue(spec.Matches(package));

            package = new PythonPackage("wood.-nano", new PackageVersion(0, 1, 1));
            Assert.IsTrue(spec.Matches(package));
        }
#endif
    }
}
