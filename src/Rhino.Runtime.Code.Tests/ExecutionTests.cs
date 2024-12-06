using System;
using System.Linq;

using NUnit.Framework;

using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using RhinoCodePlatform.Projects.Proxies;
using Rhino.Runtime.Code.Execution.Profiling;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class ExecutionTests
    {
        [OneTimeSetUp]
        public void Setup() => RhinoCode.Languages.Register(new ProxyLanguage(), Enumerable.Empty<ILanguageSpecifier>());

        [Test]
        public void Test_CompileException()
        {
            Code code = GetProxyLanguage().CreateCode("<compile-exception>");
            CompileException ex = Assert.Throws<CompileException>(() => code.Build(new RunContext()));
            Assert.AreEqual(DiagnosticSeverity.Error, ex.Diagnosis.First().Severity);
        }

        [Test]
        public void Test_CompileException_DuringRun()
        {
            Code code = GetProxyLanguage().CreateCode("<compile-exception>");
            ExecuteException ex = Assert.Throws<ExecuteException>(() => code.Run(new RunContext()));
            Assert.IsInstanceOf(typeof(CompileException), ex.InnerException);
        }

        [Test]
        public void Test_ExecuteException()
        {
            Code code = GetProxyLanguage().CreateCode("<execute-exception>");
            Assert.Throws<ExecuteException>(() => code.Run(new RunContext()));
        }

        [Test]
        public void Test_MissingDebugControls()
        {
            Code code = GetProxyLanguage().CreateCode();
            Assert.Throws<NoDebugControlsException>(() => code.Debug(new DebugContext()));
        }

        [Test]
        public void Test_HasProfiler()
        {
            Code code = GetProxyLanguage().CreateCode();
            Assert.DoesNotThrow(() => code.Profile(new ProfileContext()));
        }

        [Test]
        public void Test_MissingProfiler()
        {
            Code code = GetProxyLanguage().CreateCode();
            code.Profiler = default;
            Assert.Throws<NoProfilerException>(() => code.Profile(new ProfileContext()));
        }

        static readonly LanguageSpec s_proxyLanguage = new("*.*.proxyLang");
        static ILanguage GetProxyLanguage() => RhinoCode.Languages.QueryLatest(s_proxyLanguage);
    }
}
