using System;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code.Environments;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class PackageSpecTests
    {
#if RC8_8 && !RC9_0
        [Test]
        public void TestPackageSpecNormalizedIdMatch()
        {
            var spec = new PackageSpec("wood-nano", PackageVersionSpec.Any);

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

#if RC8_19
        class VersionGenerator
        {
            protected readonly PackageVersionSpec m_vs;
            public string Name { get; }
            public string Version { get; }
            public string VersionRule { get; }
            public VersionGenerator(string name)
            {
                Name = name;
                Version = "*";
                VersionRule = string.Empty;
                m_vs = PackageVersionSpec.Any;
            }

            public VersionGenerator(string name, string version, PackageSpec.VersionCompareRule rule)
            {
                Name = name;
                Version = version;
                VersionRule = PackageSpec.GetCompareRuleString(rule) + version;
                m_vs = new PackageVersionSpec(version);
            }

            public IEnumerable<string> GetPreVersions()
            {
                int major_prev = m_vs.Major - 1;
                yield return new PackageVersionSpec($"{major_prev}").ToString();

                int minor_prev = m_vs.HasMinor ? m_vs.Minor - 1 : 0;
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}").ToString();

                int minor = m_vs.HasMinor ? m_vs.Minor : 1;
                yield return new PackageVersionSpec($"{major_prev}.{minor}").ToString();

                int patch_prev = m_vs.HasPatch ? m_vs.Patch - 1 : 0;
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch_prev}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch_prev}").ToString();

                int patch = m_vs.HasPatch ? m_vs.Patch : 0;
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch}").ToString();

                int build_prev = m_vs.HasBuild ? m_vs.Build - 1 : 0;
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch_prev}+{build_prev}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch_prev}+{build_prev}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch}+{build_prev}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch}+{build_prev}").ToString();

                int build = m_vs.HasBuild ? m_vs.Build : 0;
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch_prev}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch_prev}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch}+{build}").ToString();

                string pre = m_vs.HasPreRelease ? m_vs.PreRelease : "beta-prev";
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch_prev}-{pre}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch_prev}-{pre}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch}-{pre}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch}-{pre}").ToString();

                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch_prev}-{pre}+{build_prev}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch_prev}-{pre}+{build_prev}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch}-{pre}+{build_prev}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch}-{pre}+{build_prev}").ToString();

                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch_prev}-{pre}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch_prev}-{pre}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor_prev}.{patch}-{pre}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_prev}.{minor}.{patch}-{pre}+{build}").ToString();
            }

            public IEnumerable<string> GetPostVersions()
            {
                int major_post = m_vs.Major + 1;
                yield return new PackageVersionSpec($"{major_post}").ToString();

                int minor = m_vs.HasMinor ? m_vs.Minor : 1;
                yield return new PackageVersionSpec($"{major_post}.{minor}").ToString();

                int minor_post = m_vs.HasMinor ? m_vs.Minor + 1 : 0;
                yield return new PackageVersionSpec($"{major_post}.{minor_post}").ToString();

                int patch = m_vs.HasPatch ? m_vs.Patch : 0;
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch}").ToString();

                int patch_post = m_vs.HasPatch ? m_vs.Patch + 1 : 0;
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch_post}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch_post}").ToString();

                int build = m_vs.HasBuild ? m_vs.Build : 0;
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch_post}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch_post}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch}+{build}").ToString();

                int build_post = m_vs.HasBuild ? m_vs.Build + 1 : 0;
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch_post}+{build_post}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch_post}+{build_post}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch}+{build_post}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch}+{build_post}").ToString();

                string pre = m_vs.HasPreRelease ? m_vs.PreRelease : "beta-post";
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch}-{pre}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch}-{pre}").ToString();

                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch_post}-{pre}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch_post}-{pre}").ToString();

                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch_post}-{pre}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch_post}-{pre}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch}-{pre}+{build}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch}-{pre}+{build}").ToString();

                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch_post}-{pre}+{build_post}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch_post}-{pre}+{build_post}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor_post}.{patch}-{pre}+{build_post}").ToString();
                yield return new PackageVersionSpec($"{major_post}.{minor}.{patch}-{pre}+{build_post}").ToString();
            }
        }

        static IEnumerable<TestCaseData> GetPackageSpecVersionCompareCases()
        {
            VersionGenerator gen;
            PackageSpec.VersionCompareRule rule;

            rule = PackageSpec.VersionCompareRule.Exact;

            gen = new VersionGenerator("compas", "1", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==_{gen.Version}=={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };

            gen = new VersionGenerator("compas", "1.1", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==_{gen.Version}=={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };

            gen = new VersionGenerator("compas", "1.8.10", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==_{gen.Version}=={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };

            gen = new VersionGenerator("compas", "1.8.10+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==_{gen.Version}=={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };

            gen = new VersionGenerator("compas", "1.8.10-beta+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==_{gen.Version}=={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"==!_{version}=={gen.Version}" };

            rule = PackageSpec.VersionCompareRule.NewerThan;

            gen = new VersionGenerator("compas", "2", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">!_{gen.Version}>{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">!_{version}>{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{version}>{gen.Version}" };

            gen = new VersionGenerator("compas", "2.1", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{gen.Version}>{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">!_{version}>{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{version}>{gen.Version}" };

            gen = new VersionGenerator("compas", "2.8.10", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{gen.Version}>{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">!_{version}>{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{version}>{gen.Version}" };

            gen = new VersionGenerator("compas", "2.8.10+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{gen.Version}>{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">!_{version}>{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{version}>{gen.Version}" };

            gen = new VersionGenerator("compas", "2.8.10-beta+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{gen.Version}>{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">!_{version}>{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">_{version}>{gen.Version}" };

            rule = PackageSpec.VersionCompareRule.NewerThanOrEqual;

            gen = new VersionGenerator("compas", "3", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{gen.Version}>={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=!_{version}>={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{version}>={gen.Version}" };

            gen = new VersionGenerator("compas", "3.1", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{gen.Version}>={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=!_{version}>={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{version}>={gen.Version}" };

            gen = new VersionGenerator("compas", "3.8.10", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{gen.Version}>={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=!_{version}>={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{version}>={gen.Version}" };

            gen = new VersionGenerator("compas", "3.8.10+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{gen.Version}>={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=!_{version}>={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{version}>={gen.Version}" };

            gen = new VersionGenerator("compas", "3.8.10-beta+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{gen.Version}>={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=!_{version}>={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $">=_{version}>={gen.Version}" };

            rule = PackageSpec.VersionCompareRule.OlderThan;

            gen = new VersionGenerator("compas", "5", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{gen.Version}<{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{version}<{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<!_{version}<{gen.Version}" };


            gen = new VersionGenerator("compas", "5.8", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{gen.Version}<{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{version}<{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<!_{version}<{gen.Version}" };

            gen = new VersionGenerator("compas", "5.8.10", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{gen.Version}<{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{version}<{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<!_{version}<{gen.Version}" };

            gen = new VersionGenerator("compas", "5.8.10+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{gen.Version}<{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{version}<{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<!_{version}<{gen.Version}" };

            gen = new VersionGenerator("compas", "5.8.10-beta+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{gen.Version}<{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<_{version}<{gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<!_{version}<{gen.Version}" };

            rule = PackageSpec.VersionCompareRule.OlderThanOrEqual;

            gen = new VersionGenerator("compas", "5", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{gen.Version}<={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{version}<={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=!_{version}<={gen.Version}" };

            gen = new VersionGenerator("compas", "5.8", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{gen.Version}<={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{version}<={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=!_{version}<={gen.Version}" };

            gen = new VersionGenerator("compas", "5.8.10", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{gen.Version}<={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{version}<={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=!_{version}<={gen.Version}" };

            gen = new VersionGenerator("compas", "5.8.10+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{gen.Version}<={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{version}<={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=!_{version}<={gen.Version}" };


            gen = new VersionGenerator("compas", "5.8.10-beta+20", rule);
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{gen.Version}<={gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=_{version}<={gen.Version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, false) { TestName = nameof(TestPackageSpec_VersionCompare) + $"<=!_{version}<={gen.Version}" };

            rule = PackageSpec.VersionCompareRule.Exact;

            gen = new VersionGenerator("compas");
            yield return new(gen.Name, gen.VersionRule, gen.Version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"*_{gen.Version}" };
            foreach (string version in gen.GetPreVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"*_{version}" };
            foreach (string version in gen.GetPostVersions())
                yield return new(gen.Name, gen.VersionRule, version, true) { TestName = nameof(TestPackageSpec_VersionCompare) + $"*_{version}" };
        }

        [Test, TestCaseSource(nameof(GetPackageSpecVersionCompareCases))]
        public void TestPackageSpec_VersionCompare(string name, string filter, string spec, bool expected)
        {
            var s = new PackageSpec(name + filter);
            var p = new PackageSpec(name, spec);
            Assert.IsTrue(s.Matches(p) == expected);
        }

        [Test]
        public void TestPackageSpec_VersionCompare_NewerThanAny()
        {
            var s = new PackageSpec("compas>=2");
            var p = new PackageSpec("compas", "2.10.0");
            Assert.IsTrue(s.Matches(p));
        }

        [Test]
        public void TestPackageSpec_VersionCompare_NewerThanAny_Other()
        {
            var s = new PackageSpec("compas>=2.10.0");
            var p = new PackageSpec("compas", "2.*");
            Assert.IsFalse(s.Matches(p));
        }

        [Test]
        public void TestPackageSpec_VersionCompare_OlderThanAny()
        {
            var s = new PackageSpec("compas<=2");
            var p = new PackageSpec("compas", "2.10.0");
            Assert.IsFalse(s.Matches(p));
        }

        [Test]
        public void TestPackageSpec_VersionCompare_OlderThanAny_Other()
        {
            var s = new PackageSpec("compas<=2.10.0");
            var p = new PackageSpec("compas", "2.*");
            Assert.IsTrue(s.Matches(p));
        }
#endif
    }
}
