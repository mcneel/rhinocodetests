using System;

using RhinoCodeEditor.Attributes;

namespace RhinoCodeEditor.Editor.Commands
{
  [CommandId("editor.testCommandError")]
  sealed class TestExceptionCommand : BaseTestCommand
  {
    public TestExceptionCommand()
    {
      Title = "Test Command Exception";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      throw new Exception("Test command error");
    }
  }
}
