using System;
using System.Text;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;

namespace RhinoCodePlatform.Rhino3D.Tests
{
    static class Utils
    {
        public static void RunCode(Code code, ExecuteContext context)
        {
            try
            {
                code.Run(context);
            }
            catch (CompileException compileEx)
            {
                var message = new StringBuilder();
                foreach (var diag in compileEx.Diagnostics)
                    message.AppendLine(diag.ToString());

                throw new Exception(message.ToString());
            }
        }
    }
}
