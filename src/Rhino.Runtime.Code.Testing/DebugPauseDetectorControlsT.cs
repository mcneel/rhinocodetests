using System;
using System.Collections;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;

namespace Rhino.Runtime.Code.Testing
{
  public abstract class DebugPauseStep
  {
    public DebugAction Action { get; }

    public DebugPauseStep(DebugAction action)
    {
      Action = action;
    }
  }

  public class ExpectedPauseEventStep : DebugPauseStep
  {
    public int Line { get; }

    public ExecEvent Event { get; }

    public ExpectedPauseEventStep(int line, ExecEvent evnt, DebugAction action) : base(action)
    {
      Line = line;
      Event = evnt;
    }
  }

  public class DebugPauseDetectorControls<TStep> : DebugControls, IEnumerable
      where TStep : DebugPauseStep
  {
    public delegate void Handler(TStep step, ExecFrame frame);

    readonly Queue<TStep> _debugSteps = default;

    public event Handler PauseOnStep;

    public DebugPauseDetectorControls() => _debugSteps = new Queue<TStep>();
    public DebugPauseDetectorControls(IEnumerable<TStep> steps) => new Queue<TStep>(steps);

    public void Add(TStep step) => _debugSteps.Enqueue(step);

    public override DebugAction Pause()
    {
      ExecFrame frame = m_results.CurrentThread.CurrentFrame;
      if (_debugSteps.Count > 0)
      {
        TStep step = _debugSteps.Dequeue();
        PauseOnStep?.Invoke(step, frame);
        return step.Action;
      }
      else
        throw new TestException($"Pausing Without Expected Step ({frame.Event} {frame.Reference.Position})");
    }

    // method must be implemented. does not do anything since
    // we don't really pause the debug
    public override void Proceed(DebugAction action) { }

    protected override void OnDetached()
    {
      base.OnDetached();

      if (_debugSteps.Count > 0)
      {
        throw new TestException($"Detached Before {_debugSteps.Count} Expected Steps");
      }
    }

    public IEnumerator GetEnumerator() => _debugSteps.GetEnumerator();
  }
}
