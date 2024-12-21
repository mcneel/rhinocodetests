using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    public sealed class DebugPauseDetectControls : DebugControls
    {
        #region Fields
        sealed class DebugPause
        {
            public bool Expected { get; }

            public CodeReferenceBreakpoint Breakpoint { get; }

            public DebugAction Action { get; }

            public DebugPause(CodeReferenceBreakpoint breakpoint, DebugAction action)
            {
                Expected = true;
                Breakpoint = breakpoint;
                Action = action;
            }

            public DebugPause(CodeReferenceBreakpoint breakpoint)
            {
                Expected = false;
                Breakpoint = breakpoint;
                Action = DebugAction.Continue;
            }
        }

        readonly Queue<DebugPause> _expected = new();
        bool _pausedOnUnexpected = false;
        #endregion

        public bool Pass => !_expected.Any(dp => dp.Expected) && !_pausedOnUnexpected;

        public DebugPauseDetectControls()
        {
        }

        public DebugPauseDetectControls(CodeReferenceBreakpoint breakpoint)
        {
            ExpectPause(breakpoint, DebugAction.Continue);
        }

        public void ExpectPause(CodeReferenceBreakpoint breakpoint, DebugAction action)
        {
            _expected.Enqueue(new DebugPause(breakpoint, action));
            Breakpoints.Add(breakpoint);
        }

        public void ExpectPause(CodeReferenceBreakpoint breakpoint)
        {
            _expected.Enqueue(new DebugPause(breakpoint, DebugAction.Continue));
            Breakpoints.Add(breakpoint);
        }

        public void DoNotExpectPause(CodeReferenceBreakpoint breakpoint)
        {
            _expected.Enqueue(new DebugPause(breakpoint));
            Breakpoints.Add(breakpoint);
        }

        public override DebugAction Pause()
        {
            if (Results.CurrentThread.CurrentFrame is ExecFrame frame)
            {
                if (ExecEvent.Line == frame.Event
                        && _expected.Count > 0
                        && _expected.Peek() is DebugPause pause
                        && pause.Breakpoint.Matches(frame))
                {
                    _expected.Dequeue();
                    _pausedOnUnexpected |= !pause.Expected;
                    return pause.Action;
                }
            }

            return DebugAction.Continue;
        }

        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }
    }
}
