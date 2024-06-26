using System;

using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
    public sealed class DebugContinueAllControls : DebugControls
    {
        public override DebugAction Pause() => DebugAction.Continue;

        // method must be implemented. does not do anything since
        // we don't really pause the debug
        public override void Proceed(DebugAction action) { }
    }
}
