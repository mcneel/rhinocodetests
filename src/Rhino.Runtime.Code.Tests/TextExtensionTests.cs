using System;

using NUnit.Framework;

using Rhino.Runtime.Code.Text;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class TextExtensionTests
    {
        [Test]
        [TestCase(new object[] { "#! python3\r\nimport rhinoscriptsyntax as rs\r\nrs.", 3, 3, 46 })]
        public void TestGetIndexFromPosition(string text, int line, int column, int expectedIndex)
        {
            Assert.True(text.TryGetIndex(new TextPosition(line, column), out int index));
            Assert.True(index == expectedIndex);
        }
    }
}
