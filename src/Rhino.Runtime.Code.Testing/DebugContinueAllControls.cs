using System;

using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    public sealed class DebugContinueAllControls : DebugControls
    {
        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }

        protected override bool IsPausingThread() => true;
        protected override DebugAction Pause() => DebugAction.Continue;
    }
}
