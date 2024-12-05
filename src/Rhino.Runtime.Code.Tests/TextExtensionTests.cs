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
        /* add: end before last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 32, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\")---;")]
        /* add: end last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 33, 3, 33, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");---")]
        /* add: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 33, 3, 100, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");---")]
        /* add: reverse range */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 1, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\")---;")]
        /* remove: beyond start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 0, 0, 0, 0, "", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* remove: start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 1, 1, 13, "", true, ";\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* remove: after start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 2, 1, 13, "", true, "u;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* remove: end before last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 32, "", true, "using System;\nusing Rhino;\n;")]
        /* remove: end last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 33, "", true, "using System;\nusing Rhino;\n")]
        /* remove: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 100, "", true, "using System;\nusing Rhino;\n")]
        /* remove->insert: reverse range */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 1, "", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: beyond start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 0, 0, 0, 10, "---", true, "---tem;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: beyond start to after start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 0, 0, 1, 10, "---", true, "---tem;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 1, 1, 13, "---", true, "---;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: after start */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 1, 2, 1, 13, "---", true, "u---;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");")]
        /* replace: end before last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 32, "---", true, "using System;\nusing Rhino;\n---;")]
        /* replace: end last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 33, "---", true, "using System;\nusing Rhino;\n---")]
        /* replace: end beyond last */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 1, 3, 100, "---", true, "using System;\nusing Rhino;\n---")]
        /* replace->insert: reverse range */
        [TestCase("using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\");", 3, 32, 3, 1, "---", true, "using System;\nusing Rhino;\nConsole.WriteLine(\"Testing CS\")---;")]
        public void TestPatch(string text, int fromLine, int fromColumn, int toLine, int toColumn, string patch, bool expected, string expectedPatched)
        {
            bool res = text.TryPatch(new TextPatch(new TextRange(fromLine, fromColumn, toLine, toColumn), patch), out string patched);
            Assert.AreEqual(expected, res);
            if (expected)
            {
                Assert.AreEqual(expectedPatched, patched);
            }
        }
#endif
    }
}
