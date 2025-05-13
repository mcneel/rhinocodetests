using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Storage;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Text;

using Grasshopper2.Data;
using Grasshopper2.Doc;
using GrasshopperIO;

namespace RhinoCodePlatform.Rhino3D.Tests
{
  [TestFixture]
  public class Grasshopper2_Tests : ScriptFixture
  {
    [Test, TestCaseSource(nameof(GetTestDefinitions))]
    public async Task TestGH2_Script(ScriptInfo scriptInfo)
    {
      TestSkip(scriptInfo);

      // NOTE:
      // somehow if this is 'Document doc', then test host
      // fails loading this assembly and test discovery
      dynamic doc = Grasshopper2_Tests_Utils.OpenDocument(scriptInfo);

      using var cts = new CancellationTokenSource(1 * 60 * 1000 /* 1 minute */);
      Solution solution = await doc.Solution.Start(cts);

      Assert.True(Grasshopper2_Tests_Utils.AssertTruthy(solution));

      if (scriptInfo.ExpectsError)
        Assert.False(Grasshopper2_Tests_Utils.AssertTruthy(doc));
      else
        Assert.True(Grasshopper2_Tests_Utils.AssertTruthy(doc));
    }

    static IEnumerable<object[]> GetTestDefinitions() => GetTestScripts(@"gh2\", "test_*.ghz");
  }

  static class Grasshopper2_Tests_Utils
  {
    static readonly Guid s_assertIoId = new("00000000-94d4-407f-b522-7a5f103b2e78");

    public static Document OpenDocument(ScriptInfo scriptInfo)
    {
      string file = scriptInfo.Uri.ToPath();
      var io = new DocumentIO(default, false, false, false);
      if (!io.Open(file) || io.Document is null)
      {
        throw new Exception("File could not be opened. Sorry about not having any more details.");
      }

      return io.Document;
    }

    public static bool AssertTruthy(Solution solution)
    {
      // There is an overload which takes a cancellation source.
      // There is also an async version called Start() which returns a task.
      // var solution = document.Solution.StartWait(); 
      switch (solution.State)
      {
        case SolutionState.Faulted:
          TestContext.WriteLine("Document solution faulted.");
          break;

        case SolutionState.Cancelled:
          TestContext.WriteLine("Document solution was cancelled.");
          break;

        case SolutionState.Completed:
          // TestContext.WriteLine($"Document solution completed in {solution.Age.TotalSeconds:0.0} seconds.");
          break;

        case SolutionState.Ending:
        case SolutionState.Running:
        case SolutionState.Required:
          TestContext.WriteLine("Document solution did not correctly run to completion.");
          break;

        default:
          TestContext.WriteLine("Unrecognised solution state: " + solution.State);
          break;
      }

      return solution.State == SolutionState.Completed;
    }

    public static bool AssertTruthy(Document document)
    {
      bool assertedTrue = false;

      IDocumentObject[] objects = document.Objects.AllObjects.ToArray();
      foreach (IDocumentObject obj in objects)
      {
        if (IO.TryGetIoId(obj.GetType(), out Guid objId)
              && objId == s_assertIoId)
        {
          assertedTrue &= AssertTruthy(obj.State.Data.Tree());
        }

        ObjectSolutionState state = obj.State;

        foreach (Message warning in state.Data.Messages.Warnings)
        {
          TestContext.WriteLine($"Warning in '{obj.Nomen.Name}': {warning.Text}");
          assertedTrue = true;
        }

        foreach (Message error in state.Data.Messages.Errors)
        {
          TestContext.WriteLine($"Error in '{obj.Nomen.Name}': {error.Text}");
          assertedTrue = true;
        }
      }

      return !assertedTrue;
    }

    static bool AssertTruthy(ITree tree)
    {
      bool assertedTrue = false;

      if (tree.TryConvert<bool>() is Tree<bool> bTree)
      {
        assertedTrue = bTree.ItemCount > 0;
        foreach (Grasshopper2.Data.Path path in tree.Paths)
        {
          Twig<bool> bTwig = bTree.Twigs[path];
          assertedTrue &= bTwig.Items.All(i => i);
        }
      }

      return assertedTrue;
    }
  }
}
