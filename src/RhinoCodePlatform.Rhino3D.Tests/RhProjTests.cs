using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Projects;
using Rhino.Runtime.Code.Storage;

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

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestSingleComponent.rhproj" })]
        public void TestRhProj_Build_Grasshopper(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), project.Settings.BuildPath.ToString());
            DeleteDirectory(rhprojfile, project.Settings.BuildPath);

            project.Identity.Version = new ProjectVersion(0, 1, 1234, 8888);
            project.Build(s_host, new NUnitProgressReporter());

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "TestSingleComponent.gha")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh8", "testsinglecomponent-0.1.1234.8888-rh8-any.yak")));

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

            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "TestRhino7Build.rhp")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "TestRhino7Build.gha")));
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
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "TestRhino7BuildWithImage.gha")));
            Assert.IsTrue(File.Exists(Path.Combine(buildPath, "rh7", "testrhino7buildwithimage-0.1.1234.8888-rh7-any.yak")));

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

            Assert.IsTrue(ex.Message.Contains("Project file is saved on Rhino 8.9 or earlier. Please re-save the project in Rhino 8.10 or above"));
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhproj", "TestOldGH.rhproj" })]
        public void TestRhProj_BuildError_OldGrasshopperFormat(string rhprojfile)
        {
            IProject project = RhinoCode.ProjectServers.CreateProject(new Uri(rhprojfile));

            ProjectBuildException ex = Assert.Throws<ProjectBuildException>(() =>
            {
                project.Build(s_host, new SilentProgressReporter());
            });

            Assert.IsTrue(ex.Message.Contains("Grasshopper file is saved on Rhino 8.9 or earlier. Please re-save the file in Rhino 8.10 or above"));

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

            Assert.IsTrue(ex.Message.Contains("Grasshopper legacy RH_IN/RH_OUT params are only supported on Rhino 8.10 or above"));
        }

        static readonly Host s_host = new("Rhino3D_TESTs", new Version(0, 1));

        static void DeleteDirectory(string rhprojfile, Uri uri)
        {
            string buildPath = Path.Combine(Path.GetDirectoryName(rhprojfile), uri.ToString());
            if (Directory.Exists(buildPath))
                Directory.Delete(buildPath, true);
        }
    }
#endif
}
