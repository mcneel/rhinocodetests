using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using NUnit.Framework;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class ClientTests
    {
        [Test]
        public void TestClient_InitPython3_FromScratch()
        {
            Process p = RunTestClient(nameof(Testing.Client.TestCases.Run_InitPython3_FromScratch));
            Assert.AreEqual(0, p.ExitCode);
        }

        [Test]
        public void TestClient_InitPython3_FromScratch_NoInternet()
        {
            Process p = RunTestClient(nameof(Testing.Client.TestCases.Run_InitPython3_FromScratch_NoInternet));
            Assert.AreEqual(0, p.ExitCode);
        }

        [Test]
        public void TestClient_InitPython2_FromScratch()
        {
            Process p = RunTestClient(nameof(Testing.Client.TestCases.Run_InitPython2_FromScratch));
            Assert.AreEqual(0, p.ExitCode);
        }

        [Test]
        public void TestClient_InitPython2_FromScratch_NoInternet()
        {
            Process p = RunTestClient(nameof(Testing.Client.TestCases.Run_InitPython2_FromScratch_NoInternet));
            Assert.AreEqual(0, p.ExitCode);
        }

        static Process RunTestClient(string args)
        {
            var pinfo = new ProcessStartInfo
            {
                FileName = "rhinocodetesting-testclient",
                Arguments = args
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                pinfo.UseShellExecute = false;

            var p = Process.Start(pinfo);
            p.WaitForExit();
            return p;
        }
    }
}
