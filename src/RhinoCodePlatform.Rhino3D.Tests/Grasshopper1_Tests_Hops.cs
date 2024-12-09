using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Grasshopper1_Tests_Hops : GH1ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestDefinitions))]
        public void TestGH1_Script_Hops(ScriptInfo scriptInfo)
        {
            TestSkip(scriptInfo);
            Test_ScriptWithWait(scriptInfo.Uri, 3);
        }

        static IEnumerable<object[]> GetTestDefinitions() => GetTestScripts(@"gh1Hops\", "test_*.gh?");
    }
}
