using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public abstract class ScriptFixture : Rhino.Testing.Fixtures.RhinoTestFixture
    {
        sealed class NUnitStream : Stream
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

        protected static bool TryRunCode(ScriptInfo scriptInfo, Code code, RunContext context, out string errorMessage)
        {
            errorMessage = default;

            if (scriptInfo.IsProfileTest)
            {
                ProfileCode(scriptInfo, code, context);
                return true;
            }

            try
            {
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
                        errorMessage = compileEx.Diagnostics.ToString();
                    else
                        errorMessage = runEx.Message;
                }
                else
                    throw;
            }

            return false;
        }

        protected static void ProfileCode(ScriptInfo scriptInfo, Code code, RunContext context)
        {
#if !RC8_9
            throw new NotImplementedException("Performance testing is not implemented for Rhino < 8.9");
#else
            // throw the first measurement out
            // that usually takes longer since the script has to build and cache
            code.Run(context);

            int rounds = scriptInfo.ProfileRounds;
            TimeSpan[] timeSpans = new TimeSpan[rounds];
            context.CollectPerformanceMetrics = true;

            for (int i = 0; i < rounds; i++)
            {
                code.Run(context);

                timeSpans[i] = context.LastExecuteTimeSpan;
                context.ResetMetrics();
            }

            if (context.OutputStream is NUnitStream stream)
            {
                stream.Flush();
                stream.Dispose();
            }

            PerfMonitor.ComputeDeviation(timeSpans, out TimeSpan meanTime, out TimeSpan stdDev);

            TimeSpan fastest = meanTime - stdDev;
            TimeSpan slowest = meanTime + stdDev;

            TestContext.WriteLine($"\"{scriptInfo.Name}\" ran {rounds} times - fastest: {fastest}, slowest: {slowest}");

            if (fastest <= scriptInfo.ExpectedFastest)
            {
                throw new Exception($"\"{scriptInfo.Name}\" is running faster than expected fastest of {scriptInfo.ExpectedFastest} (fastest: {fastest})");
            }
            else if (slowest >= scriptInfo.ExpectedSlowest)
            {
                throw new Exception($"\"{scriptInfo.Name}\" is running slower than expected slowest of {scriptInfo.ExpectedSlowest} (slowest: {slowest})");
            }
#endif
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
