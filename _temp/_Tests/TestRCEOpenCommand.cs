using System;

using Eto.Forms;

using Rhino.UI;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Storage;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;

using RhinoCodeEditor.Attributes;

namespace RhinoCodeEditor.Editor.Commands
{
  [CommandId("editor.testOpenRCEFromText")]
  sealed class TestRCEOpenTextCommand : BaseTestCommand
  {
    public TestRCEOpenTextCommand()
    {
      Title = "Test Open RCE (from Text)";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      var r = new CodeEditor(
            rce.Owner,
            new string[] {
              "#! python 3\n" +
              "import os\n" +
              "print(os)\n",
            },
            new CodeEditorOptions { IsTopMost = true }
        );

      r.Show();
    }
  }

  [CommandId("editor.testOpenRCEFromTemplate")]
  sealed class TestRCEOpenPathCommand : BaseTestCommand
  {
    public TestRCEOpenPathCommand()
    {
      Title = "Test Open RCE (from Path)";
      Description = Title;
    }

    public override async void ExecuteWith(RCE rce)
    {
      if (await rce.SelectStorageSiteWhereOpensAsync<Form>() is IStorageSite site)
      {
        try
        {
          using (IStorageSiteContext ctx = site.Open(RhinoEtoApp.MainWindow as Form, StorageSiteTheme.Empty))
          {
            if (ctx.Load("Select Script to Open") is IStorage storage)
            {
              var r = new CodeEditor(
                  rce.Owner,
                  new Uri[] { storage.Uri },
                  new CodeEditorOptions { IsTopMost = true }
                );

              r.Show();
            }
          }
        }
        catch (Exception loadEx)
        {
          RhinoCode.Logger.Error($"Error opening CodeEditor with Uri  | {loadEx}");
          rce.NotifyError("Error opening CodeEditor");
        }
      }
    }
  }

  [CommandId("editor.testOpenRCEFromTemplate")]
  sealed class TestRCEOpenTemplateCommand : BaseTestCommand
  {
    public TestRCEOpenTemplateCommand()
    {
      Title = "Test Open RCE (from Template)";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      var r = new CodeEditor(
            rce.Owner,
            new Template[] {
                new SourceCode(new LanguageSpec("*.*.python"), "Test.py", "# test")
            },
            new CodeEditorOptions { IsTopMost = true }
        );

      r.Show();
    }
  }
}
