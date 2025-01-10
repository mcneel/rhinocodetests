using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Profiling;

namespace Rhino.Runtime.Code.Testing;

public class EmptyProfilerData : ProfilerData
{
  public override void Begin() { }
  public override void BeginContext(ContextIdentity context) { }
  public override void BeginContextGroup(ContextIdentity context) { }
  public override void Trace(ExecFrame frame) { }
  public override void EndContext(ContextIdentity context) { }
  public override void End() { }
  public override void Reset() { }

  public override int GetRunCount() => 0;
  public override ContextIdentity GetLastContext() => ContextIdentity.Unknown;
  public override ContextIdentity GetLastContext(int run) => ContextIdentity.Unknown;
  public override IEnumerable<ContextIdentity> GetContexts(int run) => Enumerable.Empty<ContextIdentity>();
}

public class EmptyProfiler : Profiler
{
  public static EmptyProfiler Default { get; } = new EmptyProfiler();

  EmptyProfiler() : base(new EmptyProfilerData())
  {
  }
}
