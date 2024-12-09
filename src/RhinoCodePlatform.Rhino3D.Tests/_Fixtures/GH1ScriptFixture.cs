using System;
using System.Linq;
using System.Threading;

using NUnit.Framework;

using Rhino;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

using Grasshopper.Kernel;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    public abstract class GH1ScriptFixture : ScriptFixture
    {
        protected const string GHDOC_PARAM = "__ghdoc__";
        protected static readonly Guid s_assertTrue = new("0890a32c-4e30-4f06-a98f-ed62b45838cf");

        protected static void Test_ScriptWithWait(ScriptInfo scriptInfo, int expectedDelaySeconds)
        {
            Test_ScriptWithWait(scriptInfo.Uri, GetRunContext(scriptInfo, captureStdout: false), expectedDelaySeconds);
        }

        protected static void Test_ScriptWithWait(Uri script, int expectedDelaySeconds)
        {
            Test_ScriptWithWait(script, GetRunContext(captureStdout: false), expectedDelaySeconds);
        }

        protected static void Test_ScriptWithWait(Uri script, RunContext ctx, int expectedDelaySeconds)
        {
            const string GHDOC_PARAM = "__ghdoc__";

            Code code = GetGrasshopper().CreateCode(script);

            ctx.AutoApplyParams = true;
            ctx.Options["grasshopper.runner.asCommand"] = false;
            ctx.Options["grasshopper.runner.extractDoc"] = GHDOC_PARAM;
            ctx.Options["grasshopper.runner.skipErrors"] = true;

            code.Run(ctx);

            // wait for the async operation to be completed
            // three times the length of Task.Delay in test script
            int counter = 3 * expectedDelaySeconds * 10;
            while (counter > 0)
            {
                Thread.Sleep(100);
                counter--;

                // make sure UI works so it can pick up await continuation
                RhinoApp.Wait();
            }

            // check to make sure there are no errors
            bool hasErrors = false;
            GH_Document ghDoc = ctx.Outputs.Get<GH_Document>(GHDOC_PARAM);
            foreach (IGH_ActiveObject activeObj in ghDoc.Objects.OfType<IGH_ActiveObject>())
            {
                hasErrors |= activeObj.RuntimeMessages(GH_RuntimeMessageLevel.Error).Any();
            }

            Assert.IsFalse(hasErrors);
        }

        protected static ILanguage GetGrasshopper() => GetLanguage(new LanguageSpec(" *.*.grasshopper", "1"));
    }
}
