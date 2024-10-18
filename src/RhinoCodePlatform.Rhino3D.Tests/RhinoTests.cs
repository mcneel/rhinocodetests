using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Rhino;
using Rhino.Commands;

namespace RhinoCodePlatform.Rhino3D.Tests
{
#if RC8_11
    [TestFixture]
    public class RhinoTests : ScriptFixture
    {
        [Test]
        public void TestRunScript_NoEmptyLibs()
        {
            Assert.IsTrue(TryGetTestFile(@"rhinoPlugins\empty\TestEmptyLibs.rhp", out string rhpFile));
            RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestEmptyLibsEmpty");
            Assert.IsTrue(!Directory.Exists(Path.Combine(Path.GetDirectoryName(rhpFile), "libs")));
        }

        [Test]
        public void TestRunScript_NoEmptyLibs_Rhino7()
        {
            Assert.IsTrue(TryGetTestFile(@"rhinoPlugins\empty_rh7\TestEmptyLibs_Rhino7.rhp", out string rhpFile));
            RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestEmptyLibsEmptyRhino7");
            Assert.IsTrue(!Directory.Exists(Path.Combine(Path.GetDirectoryName(rhpFile), "libs")));
        }

        [Test]
        public void TestRunScript_TestCommandArgs()
        {
            using MemoryMappedFile mmf_cs = GetSharedMemory("TestCommandArgsCS");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "TestCommandArgsCS") == Rhino.Commands.Result.Success);
            AssertArgsReport(mmf_cs, RunMode.Interactive);

            using MemoryMappedFile mmf_py3 = GetSharedMemory("TestCommandArgsPy3");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "TestCommandArgsPy3") == Rhino.Commands.Result.Success);
            AssertArgsReport(mmf_py3, RunMode.Interactive);

            using MemoryMappedFile mmf_py2 = GetSharedMemory("TestCommandArgsPy2");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "TestCommandArgsPy2") == Rhino.Commands.Result.Success);
            AssertArgsReport(mmf_py2, RunMode.Interactive);
        }

        [Test]
        public void TestRunScript_TestCommandArgs_Script()
        {
            using MemoryMappedFile mmf_cs = GetSharedMemory("TestCommandArgsCS");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandArgsCS") == Rhino.Commands.Result.Success);
            AssertArgsReport(mmf_cs, RunMode.Scripted);

            using MemoryMappedFile mmf_py3 = GetSharedMemory("TestCommandArgsPy3");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandArgsPy3") == Rhino.Commands.Result.Success);
            AssertArgsReport(mmf_py3, RunMode.Scripted);

            using MemoryMappedFile mmf_py2 = GetSharedMemory("TestCommandArgsPy2");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandArgsPy2") == Rhino.Commands.Result.Success);
            AssertArgsReport(mmf_py2, RunMode.Scripted);
        }

        [Test]
        public void TestRunScript_TestCSharpLibWithNugetReference()
        {
            Assert.IsTrue(TryGetTestFile(@"rhinoPlugins\TestCSharpLibWithNugetReference.rhp", out string rhpFile));

            using MemoryMappedFile mmf_cs = GetSharedMemory("TestCSharpLibWithNugetReference");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCSharpLibWithNugetReference") == Rhino.Commands.Result.Success);
            Assert.IsTrue(GetReportLines(mmf_cs).Any(l => l.StartsWith("TRUE")));

            bool libfound = false;
            string libsdir = Path.Combine(Path.GetDirectoryName(rhpFile), "libs");
            foreach (var file in Directory.GetFiles(libsdir, "*.dll", SearchOption.AllDirectories))
            {
                libfound |= Path.GetFileNameWithoutExtension(file) == "LibWithPackageRef";
            }
            Assert.IsTrue(libfound);
        }
#endif

#if RC8_13
        [Test]
        public void TestRunScript_TestCommandArgs_GHCommand()
        {
            using MemoryMappedFile mmf = GetSharedMemory("TestCommandArgsGH");
            Assert.IsTrue(RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandArgsGH") == Rhino.Commands.Result.Success);
            Assert.IsTrue(GetReportLines(mmf).Any(l => l.StartsWith("TRUE")));
        }
#endif

        //[Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhino", "test_redraw.py" })]
        //public void TestRunScript_RedrawEnabled(string scriptfile)
        //{
        //    // https://mcneel.myjetbrains.com/youtrack/issue/RH-83100
        //    Assert.IsTrue(RhinoDoc.ActiveDoc.Views.RedrawEnabled);

        //    RhinoApp.RunScript($"-_ScriptEditor Run {scriptfile}", echo: true);
        //    Assert.IsTrue(RhinoDoc.ActiveDoc.Views.RedrawEnabled);
        //}

        static readonly Regex s_cmdClassNameMatcher = new("RhinoCodePlatform.Rhino3D.Projects.Plugin.ProjectCommand_.{8}");

        static void AssertArgsReport(MemoryMappedFile mmf, RunMode mode)
        {
            string[] lines = GetReportLines(mmf);

            Assert.IsTrue(s_cmdClassNameMatcher.IsMatch(lines[0]));
            Assert.AreEqual("Rhino.RhinoDoc", lines[1]);
            Assert.AreEqual(RunMode.Interactive == mode ? "Interactive" : "Scripted", lines[2]);
            Assert.AreEqual(RunMode.Interactive == mode ? "True" : "False", lines[3]);
        }

        static MemoryMappedFile GetSharedMemory(string name) => MemoryMappedFile.CreateNew(name, 10000);

        static string[] GetReportLines(MemoryMappedFile mmf)
        {
            using MemoryMappedViewStream stream = mmf.CreateViewStream();
            byte[] data = ReadAllBytes(stream);
            return Encoding.UTF8.GetString(data).Split("\n");
        }

        static byte[] ReadAllBytes(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
