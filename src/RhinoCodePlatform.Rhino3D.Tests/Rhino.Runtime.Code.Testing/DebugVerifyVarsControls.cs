#pragma warning disable IDE0075 // Simplify conditional expression
using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    delegate bool VerifyExpectedDelegate(ExecVariable expected);

    sealed class ExpectedVariable : ExecVariable
    {
        public bool ExpectsValue { get; }

        public ExpectedVariable(string id, ExecVariableKind kind = ExecVariableKind.FrameVariable, ExecVariableAttribute attribs = ExecVariableAttribute.Empty)
            : base(id, default, kind, attribs)
        {
            ExpectsValue = false;
        }

        public ExpectedVariable(string id, object value, ExecVariableKind kind = ExecVariableKind.FrameVariable, ExecVariableAttribute attribs = ExecVariableAttribute.Empty)
            : base(id, value, kind, attribs)
        {
            ExpectsValue = true;
        }
    }

    sealed class DebugVerifyVarsControls : DebugControls
    {
        readonly CodeReferenceBreakpoint _bp;
        readonly ExpectedVariable[] _expected;

        public bool Pass { get; set; } = false;

        public VerifyExpectedDelegate OnReceivedExpected;

        public DebugVerifyVarsControls(CodeReferenceBreakpoint breakpoint, IEnumerable<ExpectedVariable> expected)
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
                    ExecVariable[] vars = frame.Evaluate()
                                               .OfType<DebugExpressionVariableResult>()
                                               .Select(devr => devr.Value)
                                               .ToArray();

                    bool verify = vars.All(v => OnReceivedExpected?.Invoke(v) ?? true);
                    bool exists = _expected.All(ev => vars.Any(v => v.Id == ev.Id
                                                                 && ev.ExpectsValue ? v.ValueRepr == ev.ValueRepr : true));

                    Pass = exists && verify;
                }
            }

            return DebugAction.Continue;
        }

        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }
    }
}
