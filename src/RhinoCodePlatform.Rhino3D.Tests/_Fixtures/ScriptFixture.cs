using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

using RhinoCodePlatform.Rhino3D.Testing;

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
#if RC8_12
                ResetStreamsPolicy = ResetStreamPolicy.ResetToPlatformStream,
#endif
                AutoApplyParams = true,
                OutputStream = captureStdout ? GetOutputStream() : default,
                Outputs = {
                    ["result"] = default,
                },
            };
        }

        protected static Stream GetOutputStream() => new NUnitStream();

        protected static readonly Dispatcher s_dispatcher = new();

        protected static bool TryRunCode(ScriptInfo scriptInfo, Code code, RunContext context, out string errorMessage)
        {
            errorMessage = default;

            try
            {
#if RC8_12
                if (scriptInfo.IsAsync)
                    s_dispatcher.InvokeAsync(async () => await code.RunAsync(context)).Wait();

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

        protected static string[] RunManyExclusiveStreams(Code code, int count)
        {
            code.Inputs.Add(new Param[]
            {
                new ("a", typeof(int)),
                new ("b", typeof(int)),
            });

            code.Build(new BuildContext());

            var ts = new List<Task<string>>();
            for (int i = 0; i < count; i++)
            {
                int id = i;
                ts.Add(Task.Run(() =>
                {
                    var inputs = new ContextInputs
                    {
                        ["a"] = 21 + id,
                        ["b"] = 21 + id,
                    };

                    var outStream = new RunContextStream();
                    code.Run(new RunContext($"Execute [{i} of {count}]")
                    {
                        ExclusiveStreams = true,
                        OutputStream = outStream,
                        Inputs = inputs
                    });

                    return outStream.GetContents();
                }));
            }

            Task.WaitAll(ts.ToArray());
            return ts.Select(t => t.Result).ToArray();
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
