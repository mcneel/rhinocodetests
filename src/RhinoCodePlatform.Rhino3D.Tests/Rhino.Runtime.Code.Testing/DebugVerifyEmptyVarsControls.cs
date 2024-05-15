using System;
using System.Linq;

using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    sealed class DebugVerifyEmptyVarsControls : DebugControls
    {
        readonly CodeReferenceBreakpoint _bp;

        public bool Pass { get; set; } = false;

        public DebugVerifyEmptyVarsControls(CodeReferenceBreakpoint breakpoint)
        {
            _bp = breakpoint;

            Breakpoints.Add(breakpoint);
        }

        public override DebugAction Pause()
        {
            if (Results.CurrentThread.CurrentFrame is ExecFrame frame)
            {
                if (ExecEvent.Line == frame.Event
                        && _bp.Matches(frame))
                {
                    string[] vars = frame.Evaluate()
                                         .OfType<DebugExpressionVariableResult>()
                                         .Select(devr => devr.Value.Id)
                                         .ToArray();

                    Pass = vars.Length == 0;
                }
            }

            return DebugAction.Continue;
        }

        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }
    }
}
