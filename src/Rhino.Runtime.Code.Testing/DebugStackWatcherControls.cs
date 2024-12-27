using System;

using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    public delegate void DebugStackWatcherControlsFramePushed(ExecFrame pushed);
    public delegate void DebugStackWatcherControlsFrameSwapped(ExecFrame popped, ExecFrame pushed);
    public delegate void DebugStackWatcherControlsFramePopped(ExecFrame popped, ExecFrame returnFrame);

    public class DebugStackWatcherControls : DebugControls
    {
        public event DebugStackWatcherControlsFramePushed FramePushed;
        public event DebugStackWatcherControlsFrameSwapped FrameSwapped;
        public event DebugStackWatcherControlsFramePopped FramePopped;

        public override DebugAction Pause() => DebugAction.Continue;

        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }

        protected override void OnStackFramePushed(ExecFrame pushed) => FramePushed?.Invoke(pushed);

        protected override void OnStackFrameSwapped(ExecFrame popped, ExecFrame pushed) => FrameSwapped?.Invoke(popped, pushed);
    }
}
