using System;

using NUnit.Framework;

using Rhino.Runtime.Code.Languages;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class LanguageSpecTests : Rhino.Testing.Fixtures.RhinoTestFixture
    {
        [Test]
        public void TestLanguageSpecCtorSingleLetterFamily()
        {
            try
            {
                var spec = new LanguageSpec("d.i.f", "1.2.3");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Family can not be a single character", ex.Message);
            }
        }

        [Test]
        public void TestLanguageSpecCtorSingleLetterImplementation()
        {
            try
            {
                var spec = new LanguageSpec("d.i.ff", "1.2.3");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Implementation can not be a single character", ex.Message);
            }
        }

        [Test]
        public void TestLanguageSpecCtorSingleLetterDeveloper()
        {
            try
            {
                var spec = new LanguageSpec("d.ii.ff", "1.2.3");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Developer can not be a single character", ex.Message);
            }
        }

        [Test]
        public void TestLanguageSpecCtor()
        {
            var spec = new LanguageSpec("dev.impl.fam", "1.2.3");

            Assert.AreEqual("dev", spec.Taxon.Developer);
            Assert.AreEqual("impl", spec.Taxon.Implementation);
            Assert.AreEqual("fam", spec.Taxon.Family);

            Assert.AreEqual(1, spec.Version.Major);
            Assert.AreEqual(2, spec.Version.Minor);
            Assert.AreEqual(3, spec.Version.Patch);
        }
    }
}
