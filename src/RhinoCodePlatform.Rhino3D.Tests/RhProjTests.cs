using System;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Projects;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Storage;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class RhProjTests : ScriptFixture
    {
#if RC8_11
        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Read_Identity(string rhprojfile)
        {
            IProject project;

            void assert()
            {
                Assert.AreEqual("TestSingle", project.Identity.Name);
                Assert.AreEqual(new ProjectVersion(0, 1), project.Identity.Version);
                Assert.AreEqual("ehsan@mcneel.com", project.Identity.Publisher.Email);
                Assert.AreEqual("MIT", project.Identity.License);
            }

            project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));
            assert();

            IStorage storage = RhinoCode.StorageSites.CreateStorage(new Uri(rhprojfile));
            project = new Testing.ProjectReaderServer().CreateProject(storage);
            assert();
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Read_Settings(string rhprojfile)
        {
            IProject project;

            void assert()
            {
                Assert.AreEqual("testSingle/", project.Settings.BuildPath.ToString());
                Assert.AreEqual("Rhino3D (8.*)", project.Settings.BuildTarget.Title);
                Assert.AreEqual("McNeel Yak Server", project.Settings.PublishTarget.Title);
            }

            project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));
            assert();

            IStorage storage = RhinoCode.StorageSites.CreateStorage(new Uri(rhprojfile));
            project = new Testing.ProjectReaderServer().CreateProject(storage);
            assert();
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Read_Codes(string rhprojfile)
        {
            IProject project;

            void assert()
            {
                Assert.IsNotEmpty(project.GetCodes());

                ProjectCode code = project.GetCodes().First();
                Assert.AreEqual(new Guid("a55c3fa8-6202-45c1-8d79-e3641411fc18"), code.Id);
                Assert.AreEqual(LanguageSpec.Python, code.LanguageSpec);
                Assert.AreEqual("command", code.Title);
                Assert.IsTrue(code.Uri.IsAbsoluteUri);
            }

            project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));
            assert();

            IStorage storage = RhinoCode.StorageSites.CreateStorage(new Uri(rhprojfile));
            project = new Testing.ProjectReaderServer().CreateProject(storage);
            assert();
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestMissing.rhproj" })]
        public void TestRhProj_Read_Missing(string rhprojfile)
        {
            IProject project;

            void assert()
            {
                foreach (ProjectPath path in project.GetPaths())
                    foreach (ProjectCode code in project.GetCodes(path))
                    {
                        code.TryDiagnose(out Diagnosis diags);
                        Assert.IsNotEmpty(diags);
                        Assert.AreEqual("Script file is missing", diags.First().Message);
                    }
            }

            project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));
            assert();

            IStorage storage = RhinoCode.StorageSites.CreateStorage(new Uri(rhprojfile));
            project = new Testing.ProjectReaderServer().CreateProject(storage);
            assert();
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestMissing.rhproj" })]
        public void TestRhProj_Read_MissingUI(string rhprojfile)
        {
            // NOTE:
            // not using ProjectReaderServer here since projects read using
            // ProjectReaderServer do not report Commands/ and Components/ paths
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            var ghPathId = new Guid("9A92B0F4-AC5E-4116-A5AF-17C3BA99B5A8");
            foreach (ProjectPath path in project.GetPaths())
            {
                bool testedGHPath = false;
                IProjectShelf shelf = project.Traverse(path);
                foreach (ProjectPath shelfPath in shelf.GetPaths())
                {
                    if (shelfPath.Id == ghPathId)
                    {
                        IProjectShelf ghShelf = project.Traverse(shelfPath);
                        Assert.IsNotEmpty(ghShelf.GetPaths());
                        foreach (ProjectPath ghSource in ghShelf.GetPaths())
                        {
                            ghSource.TryDiagnose(out Diagnosis diags);
                            Assert.IsNotEmpty(diags);
                            Assert.AreEqual("Grasshopper file is missing", diags.First().Message);
                        }
                        testedGHPath = true;
                    }
                }

                Assert.IsTrue(testedGHPath);

                foreach (ProjectCode code in shelf.GetCodes())
                {
                    code.TryDiagnose(out Diagnosis diags);
                    Assert.IsNotEmpty(diags);
                    Assert.AreEqual("Script file is missing", diags.First().Message);
                }
            }
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestUnsupported.rhproj" })]
        public void TestRhProj_Read_Unsupported(string rhprojfile)
        {
            // NOTE:
            // not using ProjectReaderServer here since the exposure updates
            // on editable projects for ui only. projects read using ProjectReaderServer
            // will throw a build exception instead
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            ProjectCode code;

            code = project.GetCodes().First();
            Assert.AreEqual(new Guid("a55c3fa8-6202-45c1-8d79-e3641411fc18"), code.Id);
            Assert.AreEqual(LanguageSpec.Python, code.LanguageSpec);
            Assert.AreEqual(ProjectCodeExposure.Expose, code.Exposure);

            project.Settings.BuildTarget = new ProjectBuildTarget("Rhino3D_TESTs", new HostVersionSpec("7.*"));

            // under current implementation, after changing build target,
            // project updates code exposure when codes are queried again
            ProjectCode _ = project.GetCodes().First();
            Assert.AreEqual(ProjectCodeExposure.ExcludeUnsupported, code.Exposure);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Build_Rhino(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "TestSingle.rhp")));

#if RC8_14
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testsingle-0.1.1234+8888-rh8-any.yak")));
#else
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testsingle-0.1.1234.8888-rh8-any.yak")));
#endif

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestMultiple.rhproj" })]
        public void TestRhProj_Build_RhinoMultiple(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh8", "TestMultiple.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            using (ModuleDefinition rhp = ModuleDefinition.ReadModule(rhpFile))
            {
                TypeDefinition[] rhpTypes = rhp.Types.ToArray();
                Assert.IsNotNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_21ace57c")));
                Assert.IsNotNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_a55c3fa8")));
            }

            string ghaFile = Path.Combine(buildPath, "rh8", "TestMultiple.Components.gha");
            Assert.IsTrue(File.Exists(ghaFile));
            using (ModuleDefinition gha = ModuleDefinition.ReadModule(ghaFile))
            {
                TypeDefinition[] ghaTypes = gha.Types.ToArray();
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_26ddc562")));
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_29686ec3")));
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_d24ccf9e")));
            }

            string ruiFile = Path.Combine(buildPath, "rh8", "TestMultiple.rui");
            string rui = File.ReadAllText(ruiFile);
            Assert.IsTrue(rui.Contains("<icon guid=\"a55c3fa8-6202-45c1-8d79-e3641411fc18\">"));
            Assert.IsTrue(rui.Contains("<icon guid=\"21ace57c-eb59-45b2-8e8a-c82b6b128d36\">"));

#if RC8_14
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testmultiple-0.1.1234+8888-rh8-any.yak")));
#else
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testmultiple-0.1.1234.8888-rh8-any.yak")));
#endif

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestMultipleExcluded.rhproj" })]
        public void TestRhProj_Build_RhinoMultipleExcluded(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh8", "TestMultipleExcluded.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            using (ModuleDefinition rhp = ModuleDefinition.ReadModule(rhpFile))
            {
                TypeDefinition[] rhpTypes = rhp.Types.ToArray();
                Assert.IsNotNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_a55c3fa8")));
                Assert.IsNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_21ace57c")));
                Assert.AreEqual(1, rhpTypes.Where(t => t.Name.StartsWith("ProjectCommand_")).Count());
            }

            string ghaFile = Path.Combine(buildPath, "rh8", "TestMultipleExcluded.Components.gha");
            Assert.IsTrue(File.Exists(ghaFile));
            using (ModuleDefinition gha = ModuleDefinition.ReadModule(ghaFile))
            {
                TypeDefinition[] ghaTypes = gha.Types.ToArray();
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_26ddc562")));
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_29686ec3")));
                Assert.IsNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_d24ccf9e")));
                Assert.AreEqual(2, ghaTypes.Where(t => t.Name != "ProjectComponent_Base")
                                           .Where(t => t.Name.StartsWith("ProjectComponent_")).Count());
            }

            string ruiFile = Path.Combine(buildPath, "rh8", "TestMultipleExcluded.rui");
            string rui = File.ReadAllText(ruiFile);
            Assert.IsTrue(rui.Contains("<icon guid=\"a55c3fa8-6202-45c1-8d79-e3641411fc18\">"));
            Assert.IsFalse(rui.Contains("<icon guid=\"21ace57c-eb59-45b2-8e8a-c82b6b128d36\">"));

#if RC8_14
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testmultipleexcluded-0.1.1234+8888-rh8-any.yak")));
#else
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testmultipleexcluded-0.1.1234.8888-rh8-any.yak")));
#endif

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingleComponent.rhproj" })]
        public void TestRhProj_Build_Grasshopper(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "TestSingleComponent.Components.gha")));

#if RC8_14
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testsinglecomponent-0.1.1234+8888-rh8-any.yak")));
#else
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testsinglecomponent-0.1.1234.8888-rh8-any.yak")));
#endif

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestLibraries.rhproj" })]
        public void TestRhProj_Build_Libraries(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh8", "TestLibraries.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            TryExtractProjectFromRHP(rhpFile, out IProject rhpProj);

            ILanguageLibrary csm = rhpProj.Libraries.First(l => l.Name == "TestAssembly" && LanguageSpec.CSharp.Matches(l.LanguageSpec));
            Assert.IsNotNull(csm);
            Assert.IsNotNull(csm.GetCodes().First(c => c.Title == "Math.cs"));

            ILanguageLibrary py3m = rhpProj.Libraries.First(l => l.Name == "testmodule" && LanguageSpec.Python3.Matches(l.LanguageSpec));
            Assert.IsNotNull(py3m);
            Assert.IsNotNull(py3m.GetCodes().First(c => c.Title == "riazi.py"));

            ILanguageLibrary py2m = rhpProj.Libraries.First(l => l.Name == "testipymodule" && LanguageSpec.Python2.Matches(l.LanguageSpec));
            Assert.IsNotNull(py2m);
            Assert.IsNotNull(py2m.GetCodes().First(c => c.Title == "someData.json"));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestResources.rhproj" })]
        public void TestRhProj_Build_Resources(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh8", "TestResources.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            TryExtractProjectFromRHP(rhpFile, out IProject rhpProj);

            ProjectResource res;

            res = rhpProj.Resources.ElementAt(0);
            Assert.IsTrue(res.Id == new Guid("74324463-da92-489b-a92f-a1e9e7ca69f3"));
            Assert.IsTrue(res.Uri == new Uri("rhinocode:///projects/d8dc2a17-2626-456c-990b-c2b0c46f6174//data.txt"));

            res = rhpProj.Resources.ElementAt(1);
            Assert.IsTrue(res.Id == new Guid("353dda41-5cfd-497e-b82a-a1c6094d7a9c"));
            Assert.IsTrue(res.Uri == new Uri("rhinocode:///projects/d8dc2a17-2626-456c-990b-c2b0c46f6174//Rock.3dm"));

            res = rhpProj.Resources.ElementAt(2);
            Assert.IsTrue(res.Id == new Guid("616a5c26-9d11-4be8-a029-ce23620e62d5"));
            Assert.IsTrue(res.Uri == new Uri("rhinocode:///projects/d8dc2a17-2626-456c-990b-c2b0c46f6174//settings.ico"));

#if RC8_14
            string yakFile = Path.Combine(buildPath, "rh8", "testresources-0.1.1234+8888-rh8-any.yak");
#else
            string yakFile = Path.Combine(buildPath, "rh8", "testresources-0.1.1234.8888-rh8-any.yak");
#endif
            using (ZipArchive yak = ZipFile.Open(yakFile, ZipArchiveMode.Read))
            {
                Assert.IsNotNull(yak.Entries.First(e => e.FullName == "shared/data.txt"));
                Assert.IsNotNull(yak.Entries.First(e => e.FullName == "shared/Rock.3dm"));
                Assert.IsNotNull(yak.Entries.First(e => e.FullName == "shared/settings.ico"));
            }

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7Build.rhproj" })]
        public void TestRhProj_Build_RhinoGH7(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh7", "TestRhino7Build.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            using (ModuleDefinition rhp = ModuleDefinition.ReadModule(rhpFile))
            {
                TypeDefinition[] rhpTypes = rhp.Types.ToArray();
                Assert.IsNotNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_Python_b8e2866b")));
            }

            string ghaFile = Path.Combine(buildPath, "rh7", "TestRhino7Build.Components.gha");
            Assert.IsTrue(File.Exists(ghaFile));
            using (ModuleDefinition gha = ModuleDefinition.ReadModule(ghaFile))
            {
                TypeDefinition[] ghaTypes = gha.Types.ToArray();
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_Python_d24ccf9e")));
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_Python_f071defa")));
            }

#if RC8_14
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "testrhino7build-0.1.1234+8888-rh7-any.yak")));
#else
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "testrhino7build-0.1.1234.8888-rh7-any.yak")));
#endif

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7BuildWithImage.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_WithImages(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "TestRhino7BuildWithImage.rhp")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "TestRhino7BuildWithImage.Components.gha")));

#if RC8_14
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "testrhino7buildwithimage-0.1.1234+8888-rh7-any.yak")));
#else
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "testrhino7buildwithimage-0.1.1234.8888-rh7-any.yak")));
#endif

            string ruiFile = Path.Combine(buildPath, "rh7", "TestRhino7BuildWithImage.rui");
            Assert.IsTrue(File.Exists(ruiFile));

            string rui = File.ReadAllText(ruiFile);
            Assert.IsTrue(rui.Contains("<bitmap_item guid=\"91e208a4-5b82-4725-90e2-741b7352df75\" index=\"0\" />"));
            Assert.IsTrue(rui.Contains("<bitmap_item guid=\"b8e2866b-203f-4fa6-8702-098731dc29a1\" index=\"1\" />"));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7BuildWithGH.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_WithGrasshopper(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh7", "TestRhino7BuildWithGH.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            using (ModuleDefinition rhp = ModuleDefinition.ReadModule(rhpFile))
            {
                TypeDefinition[] rhpTypes = rhp.Types.ToArray();
                Assert.IsNotNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_Grasshopper_98265848")));
                Assert.IsNotNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_Python_e1321408")));
            }

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7BuildWithExcluded.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_WithExcluded(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh7", "TestRhino7BuildWithExcluded.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            using (ModuleDefinition rhp = ModuleDefinition.ReadModule(rhpFile))
            {
                TypeDefinition[] rhpTypes = rhp.Types.ToArray();
                Assert.IsNotNull(rhpTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectCommand_Grasshopper_98265848")));
                Assert.IsEmpty(rhpTypes.Where(t => t.Name.StartsWith("ProjectCommand_Python")));
            }

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7BuildWithExcludedComponent.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_WithExcludedComponent(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string ghaFile = Path.Combine(buildPath, "rh7", "TestRhino7BuildWithExcludedComponent.Components.gha");
            Assert.IsTrue(File.Exists(ghaFile));
            using (ModuleDefinition gha = ModuleDefinition.ReadModule(ghaFile))
            {
                TypeDefinition[] ghaTypes = gha.Types.ToArray();
                Assert.IsNotNull(ghaTypes.FirstOrDefault(t => t.Name.StartsWith("ProjectComponent_Python_d24ccf9e")));
                Assert.AreEqual(1, ghaTypes.Where(t => t.Name.StartsWith("ProjectComponent_Python")).Count());
            }

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7BuildFail.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_Fail(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            ProjectBuildException ex = Assert.Throws<ProjectBuildException>(() =>
            {
                project.Build(s_host, new SilentProgressReporter());
            });

            Assert.IsTrue(ex.Message.Contains("Rhino 7 projects can only contain Python 2 or Grasshopper commands"));
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7BuildFailWithGH.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_FailWithGH(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            ProjectBuildException ex = Assert.Throws<ProjectBuildException>(() =>
            {
                project.Build(s_host, new SilentProgressReporter());
            });

            Assert.IsTrue(ex.Message.Contains("Rhino 7 projects can only contain Python 2 or Grasshopper commands"));
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestOld.rhproj" })]
        public void TestRhProj_BuildError_OldProjectFormat(string rhprojfile)
        {
            IStorage storage = RhinoCode.StorageSites.CreateStorage(new Uri(rhprojfile));

            IProject project = RhinoCode.ProjectServers.CreateProject(storage);
            IProject projectNoHost = new Testing.ProjectReaderServer().CreateProject(storage);

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            Assert.DoesNotThrow(() =>
            {
                project.Build(s_host, new SilentProgressReporter());
            });

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            ProjectBuildException ex = Assert.Throws<ProjectBuildException>(() =>
            {
                projectNoHost.Build(s_host, new SilentProgressReporter());
            });

            Assert.IsTrue(ex.Message.Contains("Project file is saved on Rhino 8.9 or earlier. Please re-save the project in Rhino 8.11 or above"));
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestOldGH.rhproj" })]
        public void TestRhProj_BuildError_OldGrasshopperFormat(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            ProjectBuildException ex = Assert.Throws<ProjectBuildException>(() =>
            {
                project.Build(s_host, new SilentProgressReporter());
            });

            Assert.IsTrue(ex.Message.Contains("Grasshopper file is saved on Rhino 8.9 or earlier. Please re-save the file in Rhino 8.11 or above"));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestLegacyGroupParams.rhproj" })]
        public void TestRhProj_BuildError_LegacyGroupParams(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            ProjectBuildException ex = Assert.Throws<ProjectBuildException>(() =>
            {
                project.Build(s_host, new SilentProgressReporter());
            });

            Assert.IsTrue(ex.Message.Contains("Grasshopper legacy RH_IN/RH_OUT params are only supported on Rhino 8.11 or above"));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        static readonly Host s_host = new("Rhino3D_TESTs", new Version(0, 1));

        static bool TryExtractProjectFromRHP(string rhpFile, out IProject project)
        {
            project = default;

            using ModuleDefinition rhp = ModuleDefinition.ReadModule(rhpFile);
            TypeDefinition plugin = rhp.Types.First(t => t.Name == "ProjectPlugin");
            MethodDefinition cctor = plugin.Methods.First(m => m.IsConstructor && m.Attributes.HasFlag(Mono.Cecil.MethodAttributes.Static));
            ILProcessor il = cctor.Body.GetILProcessor();
            Instruction inst = il.Body.Instructions.First();
            while (inst != null)
            {
                if (inst.OpCode == OpCodes.Stsfld && ((FieldReference)inst.Operand).Name == "s_projectData")
                {
                    string encrypted = (string)inst.Previous.Operand;
                    project = RhinoCodePlatform.Projects.Rhino3DProject.DecryptProject<RhinoCodePlatform.Projects.Rhino3DProject>(encrypted);
                    return true;
                }
                inst = inst.Next;
            }

            return false;
        }

        static void DeleteDirectory(string rhprojfile, Uri uri)
        {
            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), uri.ToString());
            if (Directory.Exists(buildPath))
                Directory.Delete(buildPath, true);
        }
#endif

#if RC8_12
        [Test]
        public void TestRhProj_Create()
        {
            IProjectServer rhpServer = RhinoCode.ProjectServers.WherePasses(s_rhProjServerSpec).First();
            IProject project = rhpServer.CreateProject();

            IProjectShelf shelf = project.Traverse(project.GetPaths().First());

            IEnumerable<ProjectPath> paths = shelf.GetPaths();
            Assert.NotNull(paths.FirstOrDefault(p => p.Uri.ToString().Contains("Commands/")));
            Assert.NotNull(paths.FirstOrDefault(p => p.Uri.ToString().Contains("Components/")));
        }

        [Test]
        public void TestRhProj_Create_AddSpecificLanguage()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83675
            IProjectServer rhpServer = RhinoCode.ProjectServers.WherePasses(s_rhProjServerSpec).First();
            IProject project = rhpServer.CreateProject();

            IProjectShelf shelf = project.Traverse(project.GetPaths().First());
            ProjectPath path = shelf.GetPaths().First(p => p.Uri.ToString().Contains("Commands/"));

            project.Add(path, new SourceCode(LanguageSpec.Python2, "import sys\nprint sys.version", new Uri(Path.GetTempFileName())));
            project.Add(path, new SourceCode(LanguageSpec.Python2, "#! python 3\nimport sys\nprint(sys.version)", new Uri(Path.GetTempFileName())));

            ProjectCode command;

            command = project.GetCodes().ElementAt(0);
            Assert.AreEqual(LanguageSpec.Python2, command.LanguageSpec);

            command = project.GetCodes().ElementAt(1);
            Assert.AreEqual(LanguageSpec.Python2, command.LanguageSpec);
        }

        static readonly ProjectServerSpec s_rhProjServerSpec = new("mcneel.rhino3d.project");
#endif

#if RC8_14
        [Test]
        public void TestRhProj_ExistingCommandError_SameCode()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83900
            IProjectServer rhpServer = RhinoCode.ProjectServers.WherePasses(s_rhProjServerSpec).First();
            IProject project = rhpServer.CreateProject();

            var code = new SourceCode(LanguageSpec.Python3, "source", new Uri(Path.GetTempFileName()));
            project.Add(code);

            ProjectCode command;

            command = project.GetCodes().ElementAt(0);
            Assert.AreEqual(LanguageSpec.Python3, command.LanguageSpec);

            Assert.Throws<LibraryCodeExistsException>(() =>
            {
                project.Add(code);
            });
        }

        [Test]
        public void TestRhProj_ExistingCommandError_SameTitle()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83900
            IProjectServer rhpServer = RhinoCode.ProjectServers.WherePasses(s_rhProjServerSpec).First();
            IProject project = rhpServer.CreateProject();

            project.Add(new SourceCode(LanguageSpec.Python3, "MySource.py", "source", new Uri(Path.GetTempFileName())));

            ProjectCode command;

            command = project.GetCodes().ElementAt(0);
            Assert.AreEqual(LanguageSpec.Python3, command.LanguageSpec);

            Assert.Throws<LibraryCodeExistsException>(() =>
            {
                project.Add(new SourceCode(LanguageSpec.Python3, "MySource.py3", "other-source", new Uri(Path.GetTempFileName())));
            });
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestHiddenCommandWithIcon.rhproj" })]
        public void TestRhProj_RhinoGH8_HiddenCommandWithIconShouldNotBeInToolbar(string rhprojfile)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84578
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            Assert.AreEqual(2, project.GetCodes().Count());

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "TestHiddenCommandWithIcon.rhp")));

#if RC8_14
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testhiddencommandwithicon-0.1.1234+8888-rh8-any.yak")));
#else
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testhiddencommandwithicon-0.1.1234.8888-rh8-any.yak")));
#endif

            string ruiFile = Path.Combine(buildPath, "rh8", "TestHiddenCommandWithIcon.rui");
            Assert.IsTrue(File.Exists(ruiFile));

            string rui = File.ReadAllText(ruiFile);
            Assert.IsTrue(rui.Contains("<tool_bar_item guid=\"561b03f8-e4d9-48ff-b594-3a53e223aca8\""));
            Assert.IsTrue(rui.Contains("<icon guid=\"561b03f8-e4d9-48ff-b594-3a53e223aca8\""));

            Assert.IsFalse(rui.Contains("<tool_bar_item guid=\"a3b65e88-1deb-4068-b407-a95e084a9013\""));
            Assert.IsFalse(rui.Contains("<icon guid=\"a3b65e88-1deb-4068-b407-a95e084a9013\""));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestYakVersion.rhproj" })]
        public void TestRhProj_Build_YakVersion(string rhprojfile)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84604
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testyakversion-0.1.1234+8888-rh8-any.yak")));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestYakBetaVersion.rhproj" })]
        public void TestRhProj_Build_YakBetaVersion(string rhprojfile)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84604
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            project.Identity.Version = new ProjectVersion(0, 1, 1234, "beta", 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testyakbetaversion-0.1.1234-beta+8888-rh8-any.yak")));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }
#endif

#if RC8_15
        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestCommandHelpUri.rhproj" })]
        public void TestRhProj_Build_CommandHelpUri(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh8", "TestCommandHelpUri.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            TryExtractProjectFromRHP(rhpFile, out IProject rhpProj);

            RhinoCodePlatform.Projects.Rhino3DCommand cmd;

            cmd = rhpProj.GetCodes().OfType<RhinoCodePlatform.Projects.Rhino3DCommand>().ElementAt(0);
            Assert.AreEqual("https://www.rhino3d.com/", cmd.HelpURL.Light.ToString());

            cmd = rhpProj.GetCodes().OfType<RhinoCodePlatform.Projects.Rhino3DCommand>().ElementAt(1);
            Assert.AreEqual(string.Empty, cmd.HelpURL.Light.ToString());

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7CommandHelpUri.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_CommandHelpUri(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string rhpFile = Path.Combine(buildPath, "rh7", "TestRhino7CommandHelpUri.rhp");
            Assert.IsTrue(File.Exists(rhpFile));

            byte[] rhpBytes = File.ReadAllBytes(rhpFile);
            Assembly rhp = Assembly.Load(rhpBytes);

            Type cmdType;
            Rhino.Commands.Command cmd;
            PropertyInfo prop;

            cmdType = rhp.DefinedTypes.First(t => t.Name.StartsWith("ProjectCommand_Python_9be227a0"));
            cmd = (Rhino.Commands.Command)Activator.CreateInstance(cmdType);
            prop = cmdType.GetProperty("CommandContextHelpUrl", s_protectedFlags);
            Assert.AreEqual(string.Empty, prop.GetValue(cmd));

            cmdType = rhp.DefinedTypes.First(t => t.Name.StartsWith("ProjectCommand_Python_a9404519"));
            cmd = (Rhino.Commands.Command)Activator.CreateInstance(cmdType);
            prop = cmdType.GetProperty("CommandContextHelpUrl", s_protectedFlags);
            Assert.AreEqual("https://www.rhino3d.com/", prop.GetValue(cmd));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestCommandDefaultIcon.rhproj" })]
        public void TestRhProj_Build_CommandDefaultIcon(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            string ruiFile = Path.Combine(buildPath, "rh8", "TestCommandDefaultIcon.rui");
            string rui = File.ReadAllText(ruiFile);
            Assert.IsTrue(rui.Contains("<icon guid=\"a55c3fa8-6202-45c1-8d79-e3641411fc18\">"));
            Assert.IsTrue(rui.Contains("<icon guid=\"21ace57c-eb59-45b2-8e8a-c82b6b128d36\">"));
            Assert.IsTrue(rui.Contains("<light><svg"));
            Assert.IsTrue(rui.Contains("<dark><svg"));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }
#endif

        static readonly BindingFlags s_protectedFlags = BindingFlags.NonPublic | BindingFlags.Instance;
    }
}
