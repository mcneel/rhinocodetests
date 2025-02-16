using System;

using NUnit.Framework;

using Rhino.Runtime.Code.Languages;

using RhinoCodePlatform.Projects.Proxies;

#if RC8_18
namespace Rhino.Runtime.Code.Tests
{
  [TestFixture]
  public class PythonCaretLocationTests
  {
    public enum Caret
    {
      Comment,
      CommentBlock,
      StringLiteral,
      Code,
      Outside,
    }

    sealed class S : PythonLangugeSupport<ProxyCode>
    {
      public static Caret GetCaret(string text, int position, out char character) => (Caret)GetCaretLocation(text, position, out character);

      public override SupportCapabilities Capabilities { get; } = SupportCapabilities.Completion;

      public S() : base(new PythonLanuageSupportConfigs())
      {
      }

      protected override bool TryEvaluate<T>(SupportRequest request, ProxyCode code, string text, string token, out T value)
      {
        value = default;
        return false;
      }

      protected override bool TryEvaluate<T>(SupportRequest request, ProxyCode code, string text, string token, out System.Collections.Generic.IEnumerable<T> values)
      {
        values = default;
        return false;
      }
    }

    [Test]
    [TestCase("", 1, Caret.Outside, default)]
    [TestCase("\"", 1, Caret.Code, '\"')]
    [TestCase("\"", 2, Caret.Outside, default)]
    [TestCase("\" ", 2, Caret.StringLiteral, ' ')]
    [TestCase("\"\"", 2, Caret.StringLiteral, '\"')]
    [TestCase("\"\"", 3, Caret.Outside, default)]
    [TestCase("\"\" ", 3, Caret.Code, ' ')]
    public void TestCaret(string text, int position, Caret expectedCaret, char expectedChar)
    {
      Caret caret = S.GetCaret(text, position, out char c);
      Assert.AreEqual(expectedCaret, caret);
      Assert.AreEqual(expectedChar, c);
    }
  }
}
#endif
