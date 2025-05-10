using System;
using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests.Performance
{
    [TestFixture]
    public class TestPython3_Performance_RunGroup : ScriptFixture
    {
        RunGroup _group;
        RunContext _ctx;
        Code _code;

        [OneTimeSetUp]
        public void SetUp()
        {
            _code = GetLanguage(LanguageSpec.Python3).CreateCode("a = x + y");
            _ctx = new RunContext
            {
                AutoApplyParams = true,
                Options = { ["python.keepScope"] = true },
                Inputs = { ["x"] = default, ["y"] = default },
                Outputs = { ["a"] = default }
            };

            _group = _code.RunWith("<scope>");

            // warm up
            _ctx.Inputs.Set("x", 0);
            _ctx.Inputs.Set("y", 0);
            _code.Run(_ctx);
        }

        [OneTimeTearDown]
        public void TearDown() => _group.Dispose();

        [Test, MaxTime(600)]
        public void TestPython3_Performance_SimpleCycle_10000_RunGroup()
        {
#if RELEASE
            Assert.Ignore("Ignore performance tests on Release build");
#endif

            for (int i = 0; i < 10_000; i++)
            {
                _ctx.Inputs.Set("x", i);
                _ctx.Inputs.Set("y", i);
                _code.Run(_ctx);
            }
        }
    }
}
