using System;
using System.IO;
using System.Linq;
using System.IO.Compression;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Projects;
using Rhino.Runtime.Code.Storage;

using Mono.Cecil;
using Mono.Cecil.Cil;
using RhinoCodePlatform.Projects;

namespace RhinoCodePlatform.Rhino3D.Tests
{
#if RC8_11
    [TestFixture]
    public class RhProjTests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Read_Identity(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            Assert.AreEqual("TestSingle", project.Identity.Name);
            Assert.AreEqual(new ProjectVersion(0, 1), project.Identity.Version);
            Assert.AreEqual("ehsan@mcneel.com", project.Identity.Publisher.Email);
            Assert.AreEqual("MIT", project.Identity.License);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Read_Settings(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            Assert.AreEqual("testSingle/", project.Settings.BuildPath.ToString());
            Assert.AreEqual("Rhino3D (8.*)", project.Settings.BuildTarget.Title);
            Assert.AreEqual("McNeel Yak Server", project.Settings.PublishTarget.Title);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Read_Codes(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            Assert.IsNotEmpty(project.Codes);

            ProjectCode code = project.Codes.First();
            Assert.AreEqual(new Guid("a55c3fa8-6202-45c1-8d79-e3641411fc18"), code.Id);
            Assert.AreEqual(LanguageSpec.Python, code.LanguageSpec);
            Assert.AreEqual("command", code.Title);
            Assert.IsTrue(code.Uri.IsAbsoluteUri);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingle.rhproj" })]
        public void TestRhProj_Build_Rhino(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "TestSingle.rhp")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testsingle-0.1.1234.8888-rh8-any.yak")));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestMultiple.rhproj" })]
        public void TestRhProj_Build_RhinoMultiple(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

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

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testmultiple-0.1.1234.8888-rh8-any.yak")));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestMultipleExcluded.rhproj" })]
        public void TestRhProj_Build_RhinoMultipleExcluded(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

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

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testmultipleexcluded-0.1.1234.8888-rh8-any.yak")));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingleComponent.rhproj" })]
        public void TestRhProj_Build_Grasshopper(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "TestSingleComponent.Components.gha")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testsinglecomponent-0.1.1234.8888-rh8-any.yak")));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestLibraries.rhproj" })]
        public void TestRhProj_Build_Libraries(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

            string rhpFile = Path.Combine(buildPath, "rh8", "TestLibraries.rhp");
            Assert.IsTrue(File.Exists(rhpFile));
            TryExtractProjectFromRHP(rhpFile, out IProject rhpProj);

            ILanguageLibrary csm = rhpProj.Libraries.First(l => l.Name == "TestAssembly" && LanguageSpec.CSharp.Matches(l.LanguageSpec));
            Assert.IsNotNull(csm);
            Assert.IsNotNull(csm.Codes.First(c => c.Title == "Math.cs"));

            ILanguageLibrary py3m = rhpProj.Libraries.First(l => l.Name == "testmodule" && LanguageSpec.Python3.Matches(l.LanguageSpec));
            Assert.IsNotNull(py3m);
            Assert.IsNotNull(py3m.Codes.First(c => c.Title == "riazi.py"));

            ILanguageLibrary py2m = rhpProj.Libraries.First(l => l.Name == "testipymodule" && LanguageSpec.Python2.Matches(l.LanguageSpec));
            Assert.IsNotNull(py2m);
            Assert.IsNotNull(py2m.Codes.First(c => c.Title == "someData.json"));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestResources.rhproj" })]
        public void TestRhProj_Build_Resources(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

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

            string yakFile = Path.Combine(buildPath, "rh8", "testresources-0.1.1234.8888-rh8-any.yak");
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

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

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

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "testrhino7build-0.1.1234.8888-rh7-any.yak")));

            DeleteDirectory(rhprojfile, project.Settings.BuildPath);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestRhino7BuildWithImage.rhproj" })]
        public void TestRhProj_Build_RhinoGH7_WithImages(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "TestRhino7BuildWithImage.rhp")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "TestRhino7BuildWithImage.Components.gha")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "testrhino7buildwithimage-0.1.1234.8888-rh7-any.yak")));

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

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

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

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

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

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Build(s_host, new NUnitProgressReporter());

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

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
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

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
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

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
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
                    project = Rhino3DProject.DecryptProject<Rhino3DProject>(encrypted);
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
    }
#endif
}
