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
            public override void SetLength(long value)=> throw new NotImplementedException();

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

        protected static RunContext GetRunContext()
        {
            return new RunContext
            {
                OutputStream = GetOutputStream(),
                OverrideCodeParams = true,
                Outputs = {
                    ["result"] = default,
                },
            };
        }

        protected static Stream GetOutputStream() => new NUnitStream();

        protected static bool TryRunCode(ScriptInfo scriptInfo, Code code, RunContext context, out string errorMessage)
        {
            errorMessage = default;

            try
            {
                code.Run(context);
                return true;
            }
            catch (CompileException compileEx)
            {
                if (scriptInfo.ExpectsError)
                    errorMessage = compileEx.Diagnostics.ToString();
                else
                    throw;
            }
            catch (ExecuteException runEx)
            {
                if (scriptInfo.ExpectsError)
                    errorMessage = runEx.Message;
                else
                    throw;
            }

            return false;
        }
    
        protected static void TestSkip(ScriptInfo scriptInfo)
        {
            if (scriptInfo.IsSkipped)
                Assert.Ignore();
        }
    }
}
