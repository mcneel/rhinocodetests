#pragma warning disable CA1822, IDE0060
using System;

using Eto.Drawing;

using RhinoCodeEditor.Attributes;
using RhinoCodeEditor.Commands;

namespace RhinoCodeEditor.Editor.Commands
{
  [CommandId("editor.testCommandOverride")]
  sealed class TestRCEBindingCommand : BaseTestCommand
  {
    public TestRCEBindingCommand()
    {
      Title = "Test Command Override";
      Description = Title;
    }

    sealed class TestCommand : CodeEditorCommand
    {
      public Image Icon { get; }

      public TestCommand(CodeEditor editor)
        : base("editor.testCommand", editor, string.Empty)
      {
      }

      public override async void OnExecute(CommandArgs args)
      {
        await m_editor.PromptPickCommandAsync(new CodeEditorCommand[] {
          new TestSubCommand(m_editor, "Apple"),
          new TestSubCommand(m_editor, "Orange"),
        });
      }
    }

    sealed class TestSubCommand : CodeEditorCommand
    {
      public Image Icon { get; }

      public TestSubCommand(CodeEditor editor, string title)
        : base("editor.testSubommand", editor, string.Empty)
      {
        Title = title;
      }

      public override void OnExecute(CommandArgs args)
      {
        m_editor.ShowOk($"Running {Title}");
      }
    }

    public override void ExecuteWith(RCE rce)
    {
      var r = new CodeEditor(rce);

      r.BindCommand("editor.newCode", new TestCommand(r));

      r.Show();
    }
  }
}
