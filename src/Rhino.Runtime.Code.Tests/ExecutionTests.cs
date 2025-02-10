using System;
using System.Linq;

using NUnit.Framework;

using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
using Rhino.Runtime.Code.Testing;

using RhinoCodePlatform.Projects.Proxies;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class ExecutionTests
    {
        [OneTimeSetUp]
#if RC9_0
        public void Setup() => RhinoCode.Languages.Register(new ProxyLanguage());
#else
        public void Setup() => RhinoCode.Languages.Register(new ProxyLanguage(), Enumerable.Empty<ILanguageSpecifier>());
#endif
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

#if RC8_16
        [Test]
        public void Test_MissingDefaultProfiler()
        {
            Code code = GetProxyLanguage().CreateCode();
            Assert.Throws<NoProfilerException>(() => code.Profile(new ProfileContext()));
        }
#else
        [Test]
        public void Test_HasProfiler()
        {
            Code code = GetProxyLanguage().CreateCode();
            Assert.DoesNotThrow(() => code.Profile(new ProfileContext()));
        }
#endif

        [Test]
        public void Test_MissingProfiler()
        {
            Code code = GetProxyLanguage().CreateCode();
            code.Profiler = default;
            Assert.Throws<NoProfilerException>(() => code.Profile(new ProfileContext()));
        }

        [Test]
        public void Test_RunGroupExistsException_Debug()
        {
            Code code = GetProxyLanguage().CreateCode();
            code.DebugControls = new DebugContinueAllControls();
            RunGroupExistsException ex = Assert.Throws<RunGroupExistsException>(() =>
            {
                using RunGroup rg = code.RunWith("test");
                {
                    code.Debug(new DebugContext());
                }
            });

            Assert.AreEqual(BuildKind.Run, ex.BuildKind);
        }

        [Test]
        public void Test_RunGroupExistsException_Profile()
        {
            Code code = GetProxyLanguage().CreateCode();
            code.Profiler = EmptyProfiler.Default;
            RunGroupExistsException ex = Assert.Throws<RunGroupExistsException>(() =>
            {
                using RunGroup rg = code.RunWith("test");
                {
                    code.Profile(new ProfileContext());
                }
            });

            Assert.AreEqual(BuildKind.Run, ex.BuildKind);
        }

        [Test]
        public void Test_RunGroupExistsException_DebugGroup()
        {
            Code code = GetProxyLanguage().CreateCode();
            code.DebugControls = new DebugContinueAllControls();
            RunGroupExistsException ex = Assert.Throws<RunGroupExistsException>(() =>
            {
                using RunGroup rg = code.RunWith("test");
                {
                    code.DebugWith("testdebug");
                }
            });

            Assert.AreEqual(BuildKind.Run, ex.BuildKind);
        }

        [Test]
        public void Test_RunGroupExistsException_ProfileGroup()
        {
            Code code = GetProxyLanguage().CreateCode();
            code.Profiler = EmptyProfiler.Default;
            RunGroupExistsException ex = Assert.Throws<RunGroupExistsException>(() =>
            {
                using RunGroup rg = code.RunWith("test");
                {
                    code.ProfileWith("testdebug");
                }
            });

            Assert.AreEqual(BuildKind.Run, ex.BuildKind);
        }

        static readonly LanguageSpec s_proxyLanguage = new("*.*.proxyLang");
        static ILanguage GetProxyLanguage() => RhinoCode.Languages.QueryLatest(s_proxyLanguage);
    }
}
