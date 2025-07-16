using System;

using NUnit.Framework;

using Rhino.Runtime.Code.Text;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class TextExtensionTests
    {
        [Test]
        /* source length is 47 */
        [TestCase("#! python3\r\nimport rhinoscriptsyntax as rs\r\nrs.", 3, 3, true, 46)]
#if RC8_15
        [TestCase("#! python3\r\nimport rhinoscriptsyntax as rs\r\nrs.", 0, 0, true, 0)]
        [TestCase("#! python3\r\nimport rhinoscriptsyntax as rs\r\nrs.", 1, 1, true, 0)]
        [TestCase("#! python3\r\nimport rhinoscriptsyntax as rs\r\nrs.", 1, 2, true, 1)]
        [TestCase("#! python3\r\nimport rhinoscriptsyntax as rs\r\nrs.", 3, 100, false, -1)]
#endif

#if RC8_20
        [TestCase("#! python3\nimport rhinoscriptsyntax as rs\nrs.", 0, 0, true, 0)]
        [TestCase("#! python3\nimport rhinoscriptsyntax as rs\nrs.", 1, 1, true, 0)]
        [TestCase("#! python3\nimport rhinoscriptsyntax as rs\nrs.", 1, 2, true, 1)]
        [TestCase("#! python3\nimport rhinoscriptsyntax as rs\nrs.", 3, 100, false, -1)]
        [TestCase("// #! csharp\nusing System;\n\n", 4, 1, false, -1)]
        [TestCase("// #! csharp\nusing System;\n\n\n", 4, 1, true, 28)]
        [TestCase("// #! csharp\r\nusing System;\r\n\r\n", 4, 1, false, -1)]
        [TestCase("// #! csharp\r\nusing System;\r\n\r\n\r\n", 4, 1, true, 31)]
        [TestCase("// #! csharp\r\r\n\rusing System;\r\r\n\r\n\r\n", 4, 1, true, 34)]
#endif
        public void TestGetIndexFromPosition(string text, int line, int column, bool expected, int expectedIndex)
        {
            bool res = text.TryGetIndex(new TextPosition(line, column), out int index);
            Assert.AreEqual(expected, res);
            if (expected)
            {
                Assert.AreEqual(expectedIndex, index);
            }
        }

#if RC8_15
        [Test]
        [TestCase("doc.", 0, true, 'd')]
        [TestCase("doc.", 3, true, '.')]
        [TestCase("doc.", 4, false, '\0')]
        public void TestGetCharFromIndex(string text, int index, bool expected, char expectedChar)
        {
            bool res = text.TryGetCharacter(index, out char c);
            Assert.AreEqual(expected, res);
            if (expected)
            {
                Assert.AreEqual(expectedChar, c);
            }
        }

        [Test]
        [TestCase("doc.", 1, 1, true, 'd')]
        [TestCase("doc.", 1, 4, true, '.')]
        [TestCase("doc.", 1, 5, false, '\0')]
        public void TestGetCharFromPosition(string text, int line, int column, bool expected, char expectedChar)
        {
            bool res = text.TryGetCharacter(new TextPosition(line, column), out char c);
            Assert.AreEqual(expected, res);
            if (expected)
            {
                Assert.AreEqual(expectedChar, c);
            }
        }

        [Test]
        /* add: beyond start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 0, 0, 0, 0, "---", true, "---using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* add: start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 1, 1, 1, "---", true, "---using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* add: after start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 2, 1, 2, "---", true, "u---sing System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* add: end last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 32, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\")---;")]
        /* add: end one after last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 33, 3, 33, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");---")]
        /* add: reverse range */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 1, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\")---;")]
        /* rem beyond start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 0, 0, 0, 0, "", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* rem: start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 1, 1, 13, "", true, ";\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* rem: after start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 2, 1, 13, "", true, "u;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* rem: end last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 32, "", true, "using System;\nusing Rhino;\n;")]
        /* rem: end one after last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 33, "", true, "using System;\nusing Rhino;\n")]
        /* rem->insert: reverse range */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 1, "", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: beyond start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 0, 0, 0, 10, "---", true, "---tem;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: beyond start to after start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 0, 0, 1, 10, "---", true, "---tem;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 1, 1, 13, "---", true, "---;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: after start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 2, 1, 13, "---", true, "u---;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: end last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 32, "---", true, "using System;\nusing Rhino;\n---;")]
        /* replace: end one after last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 33, "---", true, "using System;\nusing Rhino;\n---")]
        /* replace->insert: reverse range */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 1, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\")---;")]

#if RC9_0
        /* add: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 33, 3, 100, "", false, "")]
        /* rem: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 100, "", false, "")]
        /* replace: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 100, "", false, "")]
#else
        /* add: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 33, 3, 100, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");---")]
        /* rem: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 100, "", true, "using System;\nusing Rhino;\n")]
        /* replace: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 100, "---", true, "using System;\nusing Rhino;\n---")]
#endif

        public void TestPatch(string text, int fromLine, int fromColumn, int toLine, int toColumn, string patch, bool expected, string expectedPatched)
        {
#if RC9_0
            bool res = text.TryPatch(TextPatch.Replace(new TextRange(fromLine, fromColumn, toLine, toColumn), patch), out string patched);
#else
            bool res = text.TryPatch(new TextPatch(new TextRange(fromLine, fromColumn, toLine, toColumn), patch), out string patched);
#endif
            Assert.AreEqual(expected, res);
            if (expected)
            {
                Assert.AreEqual(expectedPatched, patched);
            }
        }
#endif


        [Test]
        public void TestPatch_Grasshopper()
        {
            // this used to be test_rhinocode_textpatch.cs
            string source = @"// Grasshopper Script Instance
//#! csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

public class Script_Instance : GH_ScriptInstance
{
  /* 
    Members:
      RhinoDoc RhinoDocument
      GH_Document GrasshopperDocument
      IGH_Component Component
      int Iteration

    Methods (Virtual & overridable):
      Print(string text)
      Print(string format, params object[] args)
      Reflect(object obj)
      Reflect(object obj, string method_name)
  */
  
  private void RunScript(object x, object y, out object a)
  {
    // Write your logic here
    a = null;
  }
}
";

            // removing null in a=null assignment
#if RC9_0
            TextPatch patch = TextPatch.Remove(new TextRange(35, 9, 35, 13));
#else
            TextPatch patch = new TextPatch(new TextRange(35, 9, 35, 13), string.Empty);
#endif
            source.TryPatch(patch, out string res);
            Assert.That(!res.Contains("null"));
        }
    }
}
