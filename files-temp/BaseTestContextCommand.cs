using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Execution.Debugging;
using Rhino.Runtime.Code.Execution.Profiling;
using Rhino.Runtime.Code.Execution.Profiling.Queries;

namespace RhinoCodeEditor.Editor.Commands
{
  abstract class BaseTestContextCommand : BaseTestCommand
  {
    // NOTE:
    // cpython variable storages are thread-specific
    protected const string TEST_PY3 = @"#! python 3
from Rhino.Runtime.Code import RhinoCode as rc

x = a + b + 42
total = x

rc.Logger.Info(f""{total=}"")


class Custom:
    def Run(self, count):
        rc.Logger.Info(f""running! {count}"")
";

    protected const string TEST_PY2 = @"#! python 2
from Rhino.Runtime.Code import RhinoCode as rc

x = a + b + 42
total = x

rc.Logger.Info(""total={}"".format(x))


class Custom:
    def Run(self, count):
        rc.Logger.Info(""running! {}"".format(count))
";

    protected const string TEST_CS = @"// #! csharp
// r ""Rhino.Runtime.Code.dll""

using System;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;

x = a + b + 42;

RhinoCode.Logger.Info($""total={x}"");

public class Custom
{
  public void Run(int count) => RhinoCode.Logger.Info($""running! {count}"");
}
";

    protected static void OpenPythonCode(RCE rce, bool py3 = true)
    {
      Code code = RhinoCode.CreateCode(py3 ? TEST_PY3 : TEST_PY2);

      OpenCode(rce, code);

      code.DebugControls?.Breakpoints.Add(new CodeReferenceBreakpoint(code, 4));
    }

    protected static void OpenCSharpCode(RCE rce)
    {
      Code code = RhinoCode.CreateCode(TEST_CS);

      OpenCode(rce, code);

      code.DebugControls?.Breakpoints.Add(new CodeReferenceBreakpoint(code, 8));
    }

    static void OpenCode(RCE rce, Code code)
    {
      code.Inputs.Add(new Param[] {
        new Param("a", typeof(int)),
        new Param("b", typeof(int)),
      });

      code.Outputs.Add(new Param[] {
        new Param("x", typeof(int)),
      });

      code.Stage();
      code.Build(new BuildContext());

      rce.ActiveState.AddCode(code);

      code.Profiler?.Queries.Add(new CodeDurationQuery());
      code.Profiler?.Queries.Add(new CodeCoverageQuery());
    }

    static Task TaskRunMany(int count, Action<int> action)
    {
      var ts = new List<Task>();

      for (int i = 0; i < count; i++)
        ts.Add(Task.Run(() => action(Thread.CurrentThread.ManagedThreadId)));

      return Task.WhenAll(ts.ToArray());
    }

    protected static void Run(RCE rce)
    {
      Code code = rce.ActiveState.ActiveCode?.Code;
      if (code is null)
      {
        rce.NotifyError("Open a code first");
        return;
      }

      var inputs = new ContextInputs
      {
        ["a"] = 21,
        ["b"] = 21,
      };

      // single
      code.Run(new RunContext("Single") { Inputs = inputs });

      // same context, more than once
      var same = new RunContext($"Same Execute") { Inputs = inputs };
      code.Run(same);
      code.Run(same);

      // separate contexts
      // no data is collected under run, so there is no ExecuteScope
      // to group individual runs
      code.Run(new RunContext($"Execute (1 of 3)") { Inputs = inputs });
      code.Run(new RunContext($"Execute (2 of 3)") { Inputs = inputs });
      code.Run(new RunContext($"Execute (3 of 3)") { Inputs = inputs });
    }

    protected static async void RunMany(RCE rce, int count = 5)
    {
      Code code = rce.ActiveState.ActiveCode?.Code;
      if (code is null)
      {
        rce.NotifyError("Open a code first");
        return;
      }

      // run code many times in independent contexts
      // no data is collected under run, so there is no ExecuteScope
      // to group individual runs
      await TaskRunMany(count, (id) =>
      {
        var inputs = new ContextInputs
        {
          ["a"] = 21 + id,
          ["b"] = 21 + id,
        };

        code.Run(new RunContext($"Execute [{id} of {count}]") { Inputs = inputs });
      });
    }

    protected static void Debug(RCE rce)
    {
      Code code = rce.ActiveState.ActiveCode?.Code;
      if (code is null)
      {
        rce.NotifyError("Open a code first");
        return;
      }

      var inputs = new ContextInputs
      {
        ["a"] = 21,
        ["b"] = 21,
      };

      // single
      code.Debug(new DebugContext("Single") { Inputs = inputs });

      // same more than once
      var same = new DebugContext("Same Debug") { Inputs = inputs };
      code.Debug(same);
      code.Debug(same);

      // separate contexts
      // Grouping data is collected under debug with a scope
      using (DebugGroup shared = code.DebugWith("Debug (x of 3)"))
      {
        code.Debug(new DebugContext("Debug (1 of 3)") { Inputs = inputs });
        code.Debug(new DebugContext("Debug (2 of 3)") { Inputs = inputs });
        code.Debug(new DebugContext("Debug (3 of 3)") { Inputs = inputs });
      }
    }

    protected static async void DebugMany(RCE rce, int count = 3)
    {
      Code code = rce.ActiveState.ActiveCode?.Code;
      if (code is null)
      {
        rce.NotifyError("Open a code first");
        return;
      }

      // debug code many times in independent contexts
      // Grouping data is collected under debug with a scope
      using (DebugGroup scope = code.DebugWith($"Parallel Debug (# {count})"))
        await TaskRunMany(count, (id) =>
        {
          var inputs = new ContextInputs
          {
            ["a"] = 21 + id,
            ["b"] = 21 + id,
          };

          code.Debug(new DebugContext($"Debug [{id} of {count}]") { Inputs = inputs });
        });
    }

    protected static void Profile(RCE rce)
    {
      Code code = rce.ActiveState.ActiveCode?.Code;
      if (code is null)
      {
        rce.NotifyError("Open a script first");
        return;
      }

      var inputs = new ContextInputs
      {
        ["a"] = 21,
        ["b"] = 21,
      };

      // single
      code.Profile(new ProfileContext("Single") { Inputs = inputs });

      // same more than once
      var same = new ProfileContext("Same Profile") { Inputs = inputs };
      code.Profile(same);
      code.Profile(same);

      // separate contexts
      // Grouping data is collected under profile with a scope
      using (ProfileGroup shared = code.ProfileWith("Profile (x of 3)"))
      {
        code.Profile(new ProfileContext("Profile (1 of 3)") { Inputs = inputs });
        code.Profile(new ProfileContext("Profile (2 of 3)") { Inputs = inputs });
        code.Profile(new ProfileContext("Profile (3 of 3)") { Inputs = inputs });
      }
    }

    protected static async void ProfileMany(RCE rce, int count = 3)
    {
      Code code = rce.ActiveState.ActiveCode?.Code;
      if (code is null)
      {
        rce.NotifyError("Open a code first");
        return;
      }

      // profile code many times in independent contexts
      // Grouping data is collected under debug with a scope
      using (ProfileGroup scope = code.ProfileWith($"Parallel Profile (# {count})"))
        await TaskRunMany(count, (id) =>
        {
          var inputs = new ContextInputs
          {
            ["a"] = 21 + id,
            ["b"] = 21 + id,
          };

          code.Profile(new ProfileContext($"Single [{id}]") { Inputs = inputs });
        });
    }
  }
}
