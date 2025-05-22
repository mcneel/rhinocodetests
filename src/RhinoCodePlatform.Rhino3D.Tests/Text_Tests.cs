using System;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    [TestFixture]
    public class Text_Tests : ScriptFixture
    {
        [Test, TestCaseSource(nameof(GetTestScripts))]
        public void TestText_Script(ScriptInfo scriptInfo)
        {
            scriptInfo.TestSkip();

            // no exec for text. just make sure code can be created
            Code _ = GetLanguage(LanguageSpec.PlainText).CreateCode(scriptInfo.Uri);
        }

        static IEnumerable<object[]> GetTestScripts() => GetTestScripts(@"text\", "test_*.txt");
    }
}
