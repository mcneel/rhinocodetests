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
        public void TestRunScript_TestCommandArgs_CS()
        {
            const string name = "TestCommandArgsCS";
            using MemoryMappedFile mmf_cs = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "TestCommandArgsCS"));
            AssertArgsReport(name, mmf_cs, RunMode.Interactive);
        }

        [Test]
        public void TestRunScript_TestCommandArgs_Py3()
        {
            const string name = "TestCommandArgsPy3";
            using MemoryMappedFile mmf_py3 = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "TestCommandArgsPy3"));
            AssertArgsReport(name, mmf_py3, RunMode.Interactive);
        }

        [Test]
        public void TestRunScript_TestCommandArgs_Py2()
        {
            const string name = "TestCommandArgsPy2";
            using MemoryMappedFile mmf_py2 = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "TestCommandArgsPy2"));
            AssertArgsReport(name, mmf_py2, RunMode.Interactive);
        }

        [Test]
        public void TestRunScript_TestCommandArgs_Script_CS()
        {
            const string name = "TestCommandArgsCS";
            using MemoryMappedFile mmf_cs = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandArgsCS"));
            AssertArgsReport(name, mmf_cs, RunMode.Scripted);
        }

        [Test]
        public void TestRunScript_TestCommandArgs_Script_Py3()
        {
            const string name = "TestCommandArgsPy3";
            using MemoryMappedFile mmf_py3 = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandArgsPy3"));
            AssertArgsReport(name, mmf_py3, RunMode.Scripted);
        }

        [Test]
        public void TestRunScript_TestCommandArgs_Script_Py2()
        {
            const string name = "TestCommandArgsPy2";
            using MemoryMappedFile mmf_py2 = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandArgsPy2"));
            AssertArgsReport(name, mmf_py2, RunMode.Scripted);
        }

        [Test]
        public void TestRunScript_TestCSharpLibWithNugetReference()
        {
            Assert.IsTrue(TryGetTestFile(@"rhinoPlugins\TestCSharpLibWithNugetReference.rhp", out string rhpFile));

            using MemoryMappedFile mmf_cs = GetSharedMemory("TestCSharpLibWithNugetReference");
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCSharpLibWithNugetReference"));
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

#if RC8_14
        [Test]
        public void TestRunScript_TestCommandResult_CancelCommand_CS()
        {
            Assert.AreEqual(Result.Cancel, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-command_cancel"));
        }

        [Test]
        public void TestRunScript_TestCommandResult_CancelCommand_Py3()
        {
            Assert.AreEqual(Result.Cancel, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-command_cancel_py3"));
        }

        [Test]
        public void TestRunScript_TestCommandResult_CancelCommand_Py2()
        {
            Assert.AreEqual(Result.Cancel, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-command_cancel_py2"));
        }

        [Test]
        public void TestRunScript_TestCommandResult_CancelCommand_GH()
        {
            Assert.AreEqual(Result.Cancel, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-command_cancel_gh_py3"));
        }

        [Test]
        public void TestRunScript_TestCommandResult_CancelCommand_FromOutParam_CS()
        {
            Assert.AreEqual(Result.Cancel, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-command_cancel_outparam"));
        }

        [Test]
        public void TestRunScript_TestCommandResult_CancelCommand_FromOutParam_Py3()
        {
            Assert.AreEqual(Result.Cancel, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-command_cancel_outparam_py3"));
        }

        [Test]
        public void TestRunScript_TestCommandResult_CancelCommand_FromOutParam_Py2()
        {
            Assert.AreEqual(Result.Cancel, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-command_cancel_outparam_py2"));
        }

        [Test]
        public void TestRunScript_TestLibs_CSharpInPython_Py3()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84426
            using MemoryMappedFile mmf_py3 = GetSharedMemory("TestCSPy3");
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-TestCSharpInPython3"));
            Assert.AreEqual("TestCSharpInPython3.TestClass", GetReportLines(mmf_py3)[0][..29]);
        }

        [Test]
        public void TestRunScript_TestLibs_CSharpInPython_Py2()
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-84426
            using MemoryMappedFile mmf_py2 = GetSharedMemory("TestCSPy2");
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-TestCSharpInPython2"));
            Assert.AreEqual("<TestCSharpInPython2.TestClass", GetReportLines(mmf_py2)[0][..30]);
        }
#endif

#if RC8_15
        [Test]
        public void TestRunScript_TestCommandArgs_GH()
        {
            const string name = "TestCommandModeGH";
            using MemoryMappedFile mmf_gh = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "TestCommandModeGH"));
            AssertArgsReport(name, mmf_gh, RunMode.Interactive);
        }

        [Test]
        public void TestRunScript_TestCommandArgs_Script_GH()
        {
            const string name = "TestCommandModeGH";
            using MemoryMappedFile mmf_gh = GetSharedMemory(name);
            Assert.AreEqual(Result.Success, RhinoApp.ExecuteCommand(RhinoDoc.ActiveDoc, "-_TestCommandModeGH"));
            AssertArgsReport(name, mmf_gh, RunMode.Scripted);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhino", "test_command_args.cs" })]
        public void TestRunScript_ScriptEditorCommandArgs_Script_CS(string scriptfile)
        {
            TestRunScript_ScriptEditorCommandArgs_Script("TestScriptEditorCommandArgsCS", scriptfile);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhino", "test_command_args.py" })]
        public void TestRunScript_ScriptEditorCommandArgs_Script_PY3(string scriptfile)
        {
            TestRunScript_ScriptEditorCommandArgs_Script("TestScriptEditorCommandArgsPy3", scriptfile);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhino", "test_command_args.py2" })]
        public void TestRunScript_ScriptEditorCommandArgs_Script_PY2(string scriptfile)
        {
            TestRunScript_ScriptEditorCommandArgs_Script("TestScriptEditorCommandArgsPy2", scriptfile);
        }

        [Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhino", "test_redraw.py" })]
        public void TestRunScript_RedrawEnabled(string scriptfile)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85032
            Assert.Ignore("Awaiting fix RH-85032");

            // https://mcneel.myjetbrains.com/youtrack/issue/RH-83100
            Assert.IsTrue(RhinoDoc.ActiveDoc.Views.RedrawEnabled);
            Assert.IsTrue(RhinoApp.RunScript(RhinoDoc.ActiveDoc.RuntimeSerialNumber, $"-_ScriptEditor _Run \"{scriptfile}\"", echo: false));
            Assert.IsTrue(RhinoDoc.ActiveDoc.Views.RedrawEnabled);
        }

        static void TestRunScript_ScriptEditorCommandArgs_Script(string name, string scriptfile)
        {
            // https://mcneel.myjetbrains.com/youtrack/issue/RH-85032
            Assert.Ignore("Awaiting fix RH-85032");

            using MemoryMappedFile mmf = GetSharedMemory(name);
            Assert.IsTrue(RhinoApp.RunScript(RhinoDoc.ActiveDoc.RuntimeSerialNumber, $"-_ScriptEditor _Run \"{scriptfile}\"", echo: false));
            AssertArgsReport(name, mmf, RunMode.Scripted);
        }
#endif

        static readonly Regex s_cmdClassNameMatcher = new("RhinoCodePlatform.Rhino3D.Projects.Plugin.ProjectCommand_.{8}");

        static void AssertArgsReport(string name, MemoryMappedFile mmf, RunMode mode)
        {
            string[] lines = GetReportLines(mmf);

            TestContext.WriteLine($"< data name=\"{name}\">");
            TestContext.Write(string.Join(Environment.NewLine, lines));
            TestContext.WriteLine("</data>");

            Assert.GreaterOrEqual(lines.Length, 4);
            Assert.IsTrue(s_cmdClassNameMatcher.IsMatch(lines[0]));
            Assert.AreEqual("Rhino.RhinoDoc", lines[1]);
            Assert.AreEqual(RunMode.Interactive == mode ? "Interactive" : "Scripted", lines[2]);
            Assert.AreEqual(RunMode.Interactive == mode ? "True" : "False", lines[3]);
        }

        static MemoryMappedFile GetSharedMemory(string name) => MemoryMappedFile.CreateNew(name, 1024);

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
