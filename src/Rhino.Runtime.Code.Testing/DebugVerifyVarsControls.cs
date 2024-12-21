#pragma warning disable IDE0075 // Simplify conditional expression
using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    public delegate bool VerifyExpectedDelegate(ExecVariable expected);

    public sealed class ExpectedVariable : ExecVariable
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

    public sealed class UnexpectedVariable : ExecVariable
    {
        public UnexpectedVariable(string id, ExecVariableKind kind = ExecVariableKind.FrameVariable, ExecVariableAttribute attribs = ExecVariableAttribute.Empty)
            : base(id, default, kind, attribs)
        {
        }

        public UnexpectedVariable(string id, object value, ExecVariableKind kind = ExecVariableKind.FrameVariable, ExecVariableAttribute attribs = ExecVariableAttribute.Empty)
            : base(id, value, kind, attribs)
        {
        }
    }

    public sealed class DebugVerifyVarsControls : DebugControls
    {
        readonly CodeReferenceBreakpoint _bp;
        readonly ExpectedVariable[] _expected = Array.Empty<ExpectedVariable>();
        readonly UnexpectedVariable[] _unexpected = Array.Empty<UnexpectedVariable>();

        public bool Pass { get; set; } = false;

        public VerifyExpectedDelegate OnReceivedExpected;

        public DebugVerifyVarsControls(CodeReferenceBreakpoint breakpoint, IEnumerable<ExpectedVariable> expected)
        {
            _bp = breakpoint;
            _expected = expected.ToArray();

            Breakpoints.Add(breakpoint);
        }

        public DebugVerifyVarsControls(CodeReferenceBreakpoint breakpoint, IEnumerable<UnexpectedVariable> unexpected)
        {
            _bp = breakpoint;
            _unexpected = unexpected.ToArray();

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

                    bool verified = vars.All(v => OnReceivedExpected?.Invoke(v) ?? true);
                    bool all_expected = _expected.All(ev => vars.Any(v => v.Id == ev.Id && ev.ExpectsValue ? v.Equals(ev) : true));
                    bool no_unexpected = !_unexpected.Any(uev => vars.Any(v => v.Id == uev.Id));

                    Pass = verified && all_expected && no_unexpected;
                }
            }

            return DebugAction.Continue;
        }

        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }
    }
}
