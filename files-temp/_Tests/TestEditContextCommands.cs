using System;

using RhinoCodeEditor.Attributes;

namespace RhinoCodeEditor.Editor.Commands
{
  [CommandId("editor.testToggleEditContextDevMode")]
  sealed class TestToggleEditContextDevMode : BaseTestCommand
  {
    public TestToggleEditContextDevMode()
    {
      Title = "Test Toggle Editor Developer Mode";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      if (rce.Codes.Editors.ActiveEditContext is IWebViewEditor ectx)
        ectx.ToggleDevMode();
    }
  }
}
