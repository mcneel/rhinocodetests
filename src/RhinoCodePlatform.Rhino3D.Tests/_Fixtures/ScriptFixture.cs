using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public abstract class ScriptFixture : Rhino.Testing.Fixtures.RhinoTestFixture
    {
        protected sealed class NUnitStream : Stream
        {
            public override bool CanRead { get; } = false;
            public override bool CanSeek { get; } = false;
            public override bool CanWrite { get; } = true;
            public override long Length { get; } = 0;
            public override long Position { get; set; } = 0;

            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();

            public override void Write(byte[] buffer, int offset, int count) => TestContext.Write(Encoding.UTF8.GetString(buffer));
        }

        protected sealed class NUnitProgressReporter : ProgressReporter
        {
            protected override void WriteLine(string text) => TestContext.WriteLine(text);
        }

        protected ILanguage m_language = default;

        protected static ILanguage GetLanguage(ScriptFixture fixture, LanguageSpec languageSpec)
        {
            if (fixture.m_language is null)
            {
                fixture.m_language = RhinoCode.Languages.QueryLatest(languageSpec);
                if (fixture.m_language is null)
                {
                    throw new Exception($"Language query error | {RhinoCode.Logger.Text}");
                }
            }

            return fixture.m_language;
        }

        protected static bool TryGetTestFilesPath(out string fileDir)
        {
            Rhino.Testing.Configs configs = Rhino.Testing.Configs.Current;

            if (SetupFixture.TryGetTestFiles(out fileDir))
            {
                fileDir = Path.GetFullPath(Path.Combine(configs.SettingsDir, fileDir));
                return true;
            }

            return false;
        }

        protected static IEnumerable<string> GetTestScript(string subPath, string filename)
        {
            Rhino.Testing.Configs configs = Rhino.Testing.Configs.Current;

            if (SetupFixture.TryGetTestFiles(out string fileDir))
            {
                yield return Path.GetFullPath(Path.Combine(configs.SettingsDir, fileDir, subPath, filename));
            }
        }

        protected static IEnumerable<object[]> GetTestScripts(string subPath, string fileFilter)
        {
            Rhino.Testing.Configs configs = Rhino.Testing.Configs.Current;

            if (SetupFixture.TryGetTestFiles(out string fileDir))
            {
                string fullpath = Path.GetFullPath(Path.Combine(configs.SettingsDir, fileDir, subPath));
                if (Directory.Exists(fullpath))
                {
                    foreach (var filePath in Directory.GetFiles(fullpath, fileFilter))
                        yield return new object[] { new ScriptInfo(new Uri(filePath)) };
                }
                else
                    yield break;
            }
        }

        protected static RunContext GetRunContext(bool captureStdout = true)
        {
            return new RunContext
            {
                AutoApplyParams = true,
                OutputStream = captureStdout ? GetOutputStream() : default,
                Outputs = {
                    ["result"] = default,
                },
            };
        }

        protected static Stream GetOutputStream() => new NUnitStream();

        sealed class Dispatcher : SynchronizationContext
        {
            readonly ConcurrentQueue<Action> _queue = new();
            readonly ManualResetEventSlim _added = new(false);
            readonly Thread _t = new(Execute) { IsBackground = true };
            bool _dispatching = false;

            public Dispatcher() => _t.Start(this);

            public void Dispatch(Action action)
            {
                _dispatching = true;
                _queue.Enqueue(action);
                _added.Set();
                _t.Join();
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                _dispatching = false;
                _queue.Enqueue(() => d(state));
                _added.Set();
            }

            static void Execute(object dispatcher)
            {
                Dispatcher disp = (Dispatcher)dispatcher!;
                SetSynchronizationContext(disp);
                while (true)
                {
                    disp._added.Wait();
                    disp._added.Reset();

                    if (disp._queue.TryDequeue(out Action a))
                    {
                        a();
                        if (!disp._dispatching) break;
                    }
                }
            }
        }

        protected static bool TryRunCode(ScriptInfo scriptInfo, Code code, RunContext context, out string errorMessage)
        {
            errorMessage = default;

            try
            {
#if RC8_12
                if (scriptInfo.IsAsync)
                    new Dispatcher().Dispatch(async () => await code.RunAsync(context));

                else
#endif
                    code.Run(context);

                if (context.OutputStream is NUnitStream stream)
                {
                    stream.Flush();
                    stream.Dispose();
                }

                return true;
            }
            catch (ExecuteException runEx)
            {
                if (scriptInfo.ExpectsError || scriptInfo.ExpectsWarning)
                {
                    if (runEx.InnerException is CompileException compileEx)
#if RC8_11
                        errorMessage = compileEx.Diagnosis.ToString();
#else
                        errorMessage = compileEx.Diagnostics.ToString();
#endif
                    else
                        errorMessage = runEx.Message;
                }
                else
                    throw;
            }

            return false;
        }

        protected static void SkipBefore(int major, int minor)
        {
            Version apiVersion = typeof(Code).Assembly.GetName().Version;
            if (apiVersion.Major < major
                    || apiVersion.Minor < minor)
            {
                Assert.Ignore();
            }
        }

        protected static void TestSkip(ScriptInfo scriptInfo)
        {
            if (scriptInfo.IsSkipped)
                Assert.Ignore();
        }
    }
}
