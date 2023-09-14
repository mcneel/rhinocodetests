using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel.Data;
using NUnit.Framework;
using NUnit.Framework.Internal;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class GrasshopperTests : Testing.RhinoTestFixture
    {
        [Test, TestCaseSource(nameof(GetTestDefinitions))]
        public void TestGrasshopper1File(Uri filePath)
        {
            Eto.Platform.Initialize(Eto.Platforms.Wpf);
            RhinoCodePlatform.Rhino3D.Rhino3DPlatform.Activate();

            ILanguage gh1 = RhinoCode.Languages.QueryLatest(new LanguageSpec("*.*.grasshopper", "1"));
            Assert.NotNull(gh1);

            var code = gh1.CreateCode(filePath);

            var ctx = new ExecuteContext
            {
                Outputs = {
                    ["result"] = default,
                },

                Options = {
                    ["grasshopper.runAsCommand"] = false
                }
            };

            code.Run(ctx);

            Assert.True(ctx.Outputs.TryGet("result", out IGH_Structure data));
            foreach (var p in data.Paths)
            {
                foreach (var d in data.get_Branch(p))
                    if (d is bool result)
                        Assert.True(result);
            }
        }

        static IEnumerable<object[]> GetTestDefinitions()
        {
            if (Configs.TryGetConfig("Grasshopper1FilesDirectory", out string gh1FilesDir))
            {
                string fullpath = Path.GetFullPath(Path.Combine(Configs.SettingsDir, @"..\..\..\", gh1FilesDir));
                if (Directory.Exists(fullpath))
                {
                    foreach (var filePath in Directory.GetFiles(fullpath, "*.gh"))
                        yield return new object[] { new Uri(filePath) };
                }
                else
                    yield break;
            }
        }
    }
}
