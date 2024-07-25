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

using RhinoCodePlatform.Rhino3D.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
#if RC9_0
    [TestFixture]
    public class PythonScript_Tests : ScriptFixture
    {
        [Test]
        public void TestPythonScript3()
        {
            var script = new Python3Script();
            script.ExecuteScript(@"");
        }
    }
#endif
}
