using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code.Languages;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class LanguageSpecTests
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
        public void TestLanguageSpecCtorStarFamily()
        {
            try
            {
                var spec = new LanguageSpec("dd.*");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Implementation value can not be more stringent than previous filter", ex.Message);
            }

            try
            {
                var spec = new LanguageSpec("dd.ii.*");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Implementation value can not be more stringent than previous filter", ex.Message);
            }
        }

        [Test]
        public void TestLanguageSpecCtorStarImplementation()
        {
            try
            {
                var spec = new LanguageSpec("dd.*.*");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Developer value can not be more stringent than previous filter", ex.Message);
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

        sealed class LanguageSpecComparer : IComparer<LanguageSpec>
        {
            public int Compare(LanguageSpec x, LanguageSpec y)
            {
                if (x.Taxon.Family == y.Taxon.Family)
                    return x.Version.CompareTo(y.Version);

                // sort pythons first
                return x.Taxon.Family == "python" ? 1 : -1;
            }
        }

        [Test]
        public void TestLanguageSpecOrder()
        {
            LanguageSpec[] specs = new LanguageSpec[] {
                new ("*.*.csharp", "9"),
                new ("*.*.python", "3"),
                new ("*.ironpython.python", "2.7.12"),
                new ("*.pythonnet.python", "3.9.10"),
            }.OrderByDescending(m => m, new LanguageSpecComparer()).ToArray();

            LanguageSpec spec;

            //*.pythonnet.python@3.9.10
            spec = specs[0];
            Assert.AreEqual("python", spec.Taxon.Family);
            Assert.AreEqual("pythonnet", spec.Taxon.Implementation);
            Assert.AreEqual(3, spec.Version.Major);
            Assert.AreEqual(9, spec.Version.Minor);
            Assert.AreEqual(10, spec.Version.Patch);

            //*.*.python@3.*
            spec = specs[1];
            Assert.AreEqual("python", spec.Taxon.Family);
            Assert.AreEqual("*", spec.Taxon.Implementation);
            Assert.AreEqual(3, spec.Version.Major);
            Assert.AreEqual(-1, spec.Version.Minor);

            //*.ironpython.python@2.7.12
            spec = specs[2];
            Assert.AreEqual("python", spec.Taxon.Family);
            Assert.AreEqual("ironpython", spec.Taxon.Implementation);
            Assert.AreEqual(2, spec.Version.Major);
            Assert.AreEqual(7, spec.Version.Minor);
            Assert.AreEqual(12, spec.Version.Patch);

            //*.*.csharp@9.*
            spec = specs[3];
            Assert.AreEqual("csharp", spec.Taxon.Family);
            Assert.AreEqual("*", spec.Taxon.Implementation);
            Assert.AreEqual(9, spec.Version.Major);
            Assert.AreEqual(-1, spec.Version.Minor);
        }

        [Test]
        public void TestLanguageSpecParse()
        {
            LanguageSpec spec;

            // without taxon:version separator
            Assert.IsTrue(LanguageSpec.TryParse("python3", out spec));
            Assert.AreEqual(new LanguageSpec("python", "3"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("*.python3.9", out spec));
            Assert.AreEqual(new LanguageSpec("*.python", "3.9"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("mcneel.python3.9.10-dev", out spec));
            Assert.AreEqual(new LanguageSpec("mcneel.python", "3.9.10"), spec);

            // with taxon:version separator (:@-\s)
            Assert.IsTrue(LanguageSpec.TryParse("gh1.csharp: 12.4-testing", out spec));
            Assert.AreEqual(new LanguageSpec("gh1.csharp", "12.4"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("*.net.python@3.9", out spec));
            Assert.AreEqual(new LanguageSpec("*.net.python", "3.9"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("*.python-3.10.*", out spec));
            Assert.AreEqual(new LanguageSpec("*.python", "3.10.*"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("mcneel.python 3.9", out spec));
            Assert.AreEqual(new LanguageSpec("mcneel.python", "3.9"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("python@*", out spec));
            Assert.AreEqual(new LanguageSpec("python"), spec);

            // no version
            Assert.IsTrue(LanguageSpec.TryParse("python", out spec));
            Assert.AreEqual(new LanguageSpec("python"), spec);
            
            Assert.IsTrue(LanguageSpec.TryParse("gh1.csharp", out spec));
            Assert.AreEqual(new LanguageSpec("gh1.csharp"), spec);

            // special cases
            Assert.IsFalse(LanguageSpec.TryParse("python-*_custom@sep", out spec));
            Assert.IsFalse(LanguageSpec.TryParse("python-3.*@custom", out spec));
            Assert.IsFalse(LanguageSpec.TryParse("python-3.*:custom", out spec));

            Assert.IsTrue(LanguageSpec.TryParse("python@*_custom", out spec));
            Assert.AreEqual(new LanguageSpec("python"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("python-3.*_custom", out spec));
            Assert.AreEqual(new LanguageSpec("python", "3"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("python-3.*-custom", out spec));
            Assert.AreEqual(new LanguageSpec("python", "3"), spec);

            Assert.IsTrue(LanguageSpec.TryParse("python-3.* custom", out spec));
            Assert.AreEqual(new LanguageSpec("python", "3"), spec);
        }
    }
}
