using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    sealed class DebugVerifyVarsControls : DebugControls
    {
        readonly CodeReferenceBreakpoint _bp;
        readonly ExecVariable[] _expected;

        public bool Pass { get; set; } = false;

        public DebugVerifyVarsControls(CodeReferenceBreakpoint breakpoint, IEnumerable<ExecVariable> expected)
        {
            _bp = breakpoint;
            _expected = expected.ToArray();

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
                    
                    Pass = _expected.All(ev => vars.Contains(ev.Id));
                }
            }

            return DebugAction.Continue;
        }

        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }
    }
}
