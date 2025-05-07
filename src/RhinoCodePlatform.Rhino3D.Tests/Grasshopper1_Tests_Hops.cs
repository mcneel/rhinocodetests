using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

using NUnit.Framework;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Grasshopper1_Tests_Hops : GH1ScriptFixture
    {
        [Test]
        public void TestGH1_Script_Hops_Configurations()
        {
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string ghappdata = Path.Combine(appdata, "Grasshopper");
            string ghsettings = Path.Combine(ghappdata, "grasshopper_kernel.xml");

            Assert.True(File.Exists(ghsettings));
            string settings = File.ReadAllText(ghsettings);
            TestContext.Write(settings);
            Assert.True(settings.Contains("http://localhost:5000"));
        }

        [Test]
        public void TestGH1_Script_Hops_HealthCheck()
        {
            TestSkipHops();
        }

        [Test, TestCaseSource(nameof(GetTestDefinitions))]
        public void TestGH1_Script_Hops(ScriptInfo scriptInfo)
        {
            TestSkipHops();
            TestSkip(scriptInfo);
            Test_ScriptWithWait(scriptInfo.Uri, 3);
        }

        static void TestSkipHops()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create("http://localhost:5000/healthcheck");
                req.Timeout = 5000;
                req.Method = "GET";

                using var resp = (HttpWebResponse)req.GetResponse();
                if (resp.StatusCode == HttpStatusCode.OK)
                    return;
            }
            catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
            {
                Assert.Fail("Failed hops test due to timeout exception");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed hops test due to exception | {ex}");
            }

            Assert.Fail("Failed hops test due to healthcheck error");
        }

        static IEnumerable<object[]> GetTestDefinitions() => GetTestScripts(@"gh1Hops\", "test_*.gh?");
    }
}
