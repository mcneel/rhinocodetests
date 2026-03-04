#pragma warning disable IDE0075 // Simplify conditional expression
using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    public delegate bool VerifyExpectedDelegate(ExecSlot expected);

    public sealed class ExpectedVariable : ExecSlot
    {
        public bool ExpectsValue { get; }

        public ExpectedVariable(string id, ExecSlotKind kind = ExecSlotKind.FrameSlot, ExecSlotAccess access = ExecSlotAccess.Default)
            : base(new ExecSlotIdentity(kind, access, id), ExecValue.None(ExecExpression.Empty))
        {
            ExpectsValue = false;
        }

        public ExpectedVariable(string id, object value, ExecSlotKind kind = ExecSlotKind.FrameSlot, ExecSlotAccess access = ExecSlotAccess.Default)
            : base(new ExecSlotIdentity(kind, access, id), new ExecValue(ExecExpression.Empty, value))
        {
            ExpectsValue = true;
        }
    }

    public sealed class UnexpectedVariable : ExecSlot
    {
        public UnexpectedVariable(string id, ExecSlotKind kind = ExecSlotKind.FrameSlot, ExecSlotAccess access = ExecSlotAccess.Default)
            : base(new ExecSlotIdentity(kind, access, id), ExecValue.None(ExecExpression.Empty))
        {
        }

        public UnexpectedVariable(string id, object value, ExecSlotKind kind = ExecSlotKind.FrameSlot, ExecSlotAccess access = ExecSlotAccess.Default)
            : base(new ExecSlotIdentity(kind, access, id), new ExecValue(ExecExpression.Empty, value))
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

        protected override bool IsPausingThread() => true;
        protected override DebugAction Pause()
        {
            if (Results.CurrentThread.CurrentFrame is ExecFrame frame)
            {
                if (ExecEvent.Line == frame.Event
                        && _bp.Matches(frame))
                {
                    ExecSlot[] vars = frame.GetSlots().ToArray();
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
