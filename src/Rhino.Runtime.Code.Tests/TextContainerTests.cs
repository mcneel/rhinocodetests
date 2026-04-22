using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using NUnit.Framework;

using Rhino.Runtime.Code.Text;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class TextContainerTests
    {
        sealed class Text : TextContainer
        {
            protected override void OnTextChanged(TextChange args) { }

            protected override void OnTextReadOnlyChanged() { }
        }

        [Test, TestCaseSource(nameof(GetTestSearchCases))]
        public void TestSearch(string text, TextSearchCriteria criteria, TextSearchMatch[] em)
        {
            var t = new Text();
            t.Set(text);

            TextSearchMatch[] matches = t.Search(criteria).ToArray();
            Assert.AreEqual(em.Length, matches.Length);
            for (int i = 0; i < em.Length; i++)
            {
                Assert.AreEqual(em[i], matches[i]);
            }
        }
        static IEnumerable<TestCaseData> GetTestSearchCases()
        {
            string s;
            TextSearchCriteria c;

            s = "#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)";
            c = new TextSearchCriteria("#!");
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(1, 1, 1, 3), c, "#!"),
            });

            c = new TextSearchCriteria("rs");
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(2, 29, 2, 31), c, "rs"),
                new (new TextRange(3, 07, 3, 09), c, "rs")
            });

            s = "#! python3\r\nimport rhinoscriptsyntax as rs\r\n\r\n# print_statement\r\nprint(rs)";
            c = new TextSearchCriteria("print");
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(4, 3, 4, 8), c, "print"),
                new (new TextRange(5, 1, 5, 6), c, "print"),
            });

            c = new TextSearchCriteria(new Regex(@"\bprint\b"));
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(5, 1, 5, 6), c, "print"),
            });

            s = "# print_statement\r\nprint()\r\n\r\n# PRInt_statement\r\nPRInt()\r\n\r\n# PRINT_statement\r\nPRINT()\r\n";
            c = new TextSearchCriteria("print");
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(1, 3, 1, 8), c, "print"),
                new (new TextRange(2, 1, 2, 6), c, "print"),
                new (new TextRange(4, 3, 4, 8), c, "PRInt"),
                new (new TextRange(5, 1, 5, 6), c, "PRInt"),
                new (new TextRange(7, 3, 7, 8), c, "PRINT"),
                new (new TextRange(8, 1, 8, 6), c, "PRINT"),
            });

            c = new TextSearchCriteria("print", isCaseSensitive: true);
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(1, 3, 1, 8), c, "print"),
                new (new TextRange(2, 1, 2, 6), c, "print"),
            });

            c = new TextSearchCriteria(new Regex(@"\bprint\b"));
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(2, 1, 2, 6), c, "print"),
                new (new TextRange(5, 1, 5, 6), c, "PRInt"),
                new (new TextRange(8, 1, 8, 6), c, "PRINT"),
            });

            c = new TextSearchCriteria(new Regex(@"\bprint\b"), isCaseSensitive: true);
            yield return new(s, c, new TextSearchMatch[]
            {
                new (new TextRange(2, 1, 2, 6), c, "print"),
            });
        }

        [Test, TestCaseSource(nameof(GetTestReplaceFirstCases))]
        public void TestReplaceFirst(string text, TextSearchReplaceCriteria criteria, string replaced)
        {
            var t = new Text();
            t.Set(text);

            Assert.IsTrue(t.ReplaceFirst(criteria));
            Assert.AreEqual(replaced, (string)t);
        }
        static IEnumerable<TestCaseData> GetTestReplaceFirstCases()
        {
            yield return new("#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)", new TextSearchReplaceCriteria("rs", "RSC"), "#! python3\nimport rhinoscriptsyntax as RSC\nprint(rs)");
        }

        [Test, TestCaseSource(nameof(GetTestReplaceFirstFromCases))]
        public void TestReplaceFirstFrom(string text, TextPosition position, TextSearchReplaceCriteria criteria, string replaced)
        {
            var t = new Text();
            t.Set(text);

            Assert.IsTrue(t.ReplaceFirst(position, criteria));
            Assert.AreEqual(replaced, (string)t);
        }
        static IEnumerable<TestCaseData> GetTestReplaceFirstFromCases()
        {
            yield return new("#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)", new TextPosition(1, 01), new TextSearchReplaceCriteria("rs", "RSC"), "#! python3\nimport rhinoscriptsyntax as RSC\nprint(rs)");
            yield return new("#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)", new TextPosition(2, 29), new TextSearchReplaceCriteria("rs", "RSC"), "#! python3\nimport rhinoscriptsyntax as RSC\nprint(rs)");
            yield return new("#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)", new TextPosition(2, 29 + 1), new TextSearchReplaceCriteria("rs", "RSC"), "#! python3\nimport rhinoscriptsyntax as rs\nprint(RSC)");
        }

        [Test, TestCaseSource(nameof(GetTestReplaceAnyCases))]
        public void TestReplaceAny(string text, TextSearchReplaceCriteria criteria, string replaced)
        {
            var t = new Text();
            t.Set(text);

            Assert.IsTrue(t.Replace(criteria));
            Assert.AreEqual(replaced, (string)t);
        }
        static IEnumerable<TestCaseData> GetTestReplaceAnyCases()
        {
            yield return new("#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)", new TextSearchReplaceCriteria("rs", "RSC"), "#! python3\nimport rhinoscriptsyntax as RSC\nprint(RSC)");
        }

        [Test, TestCaseSource(nameof(GetTestReplaceCases))]
        public void TestReplace(string text, TextSearchReplaceCriteria criteria, int expected, string replaced)
        {
            var t = new Text();
            t.Set(text);

            TextSearchMatch[] matches = t.Search(criteria).ToArray();
            Assert.AreEqual(expected, matches.Length);
            Assert.IsTrue(t.Replace(matches));
            Assert.AreEqual(replaced, (string)t);
        }
        static IEnumerable<TestCaseData> GetTestReplaceCases()
        {
            yield return new("#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)", new TextSearchReplaceCriteria("rs", "RSC"), 2, "#! python3\nimport rhinoscriptsyntax as RSC\nprint(RSC)");
        }

        [Test, TestCaseSource(nameof(GetTestReplaceLastCases))]
        public void TestReplaceLast(string text, TextSearchReplaceCriteria criteria, int expected, string replaced)
        {
            var t = new Text();
            t.Set(text);

            TextSearchMatch[] matches = t.Search(criteria).ToArray();
            Assert.AreEqual(expected, matches.Length);
            Assert.IsTrue(t.Replace(matches.Last()));
            Assert.AreEqual(replaced, (string)t);
        }
        static IEnumerable<TestCaseData> GetTestReplaceLastCases()
        {
            yield return new("#! python3\nimport rhinoscriptsyntax as rs\nprint(rs)", new TextSearchReplaceCriteria("rs", "RSC"), 2, "#! python3\nimport rhinoscriptsyntax as rs\nprint(RSC)");
        }
    }
}
