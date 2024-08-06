using System;
using System.IO;
using System.Linq;
using System.IO.Compression;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Projects;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Storage;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Rhino;

namespace RhinoCodePlatform.Rhino3D.Tests
{
#if RC8_11
    [TestFixture]
    public class RhinoTests : ScriptFixture
    {
        //[Test, TestCaseSource(nameof(GetTestScript), new object[] { "rhino", "test_redraw.py" })]
        //public void TestRunScript_RedrawEnabled(string scriptfile)
        //{
        //    // https://mcneel.myjetbrains.com/youtrack/issue/RH-83100
        //    Assert.IsTrue(RhinoDoc.ActiveDoc.Views.RedrawEnabled);

        //    RhinoApp.RunScript($"-_ScriptEditor Run {scriptfile}", echo: true);
        //    Assert.IsTrue(RhinoDoc.ActiveDoc.Views.RedrawEnabled);
        //}
    }
#endif
}
