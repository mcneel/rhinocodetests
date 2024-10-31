using System;

using Eto.Forms;

using Rhino.UI;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Text;
using Rhino.Runtime.Code.Editing;

using RhinoCodeEditor.Attributes;

#pragma warning disable CA1822, IDE0060
namespace RhinoCodeEditor.Editor.Commands
{
  [CommandId("editor.testOkMessage")]
  sealed class TestOkMessageCommand : BaseTestCommand
  {
    public TestOkMessageCommand()
    {
      Title = "Test Ok Message";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      rce.ShowOk("Testing ok message on status bar");
    }
  }

  [CommandId("editor.testErrorMessage")]
  sealed class TestErrorMessageCommand : BaseTestCommand
  {
    public TestErrorMessageCommand()
    {
      Title = "Test Error Message";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      RhinoCode.Logger.Error("Testing error message in logger");
      rce.ShowError("Testing error message on status bar");
    }
  }

  [CommandId("editor.testWarningMessage")]
  sealed class TestWarningMessageCommand : BaseTestCommand
  {
    public TestWarningMessageCommand()
    {
      Title = "Test Warning Message";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      RhinoCode.Logger.Warn("Testing warning message in logger");
      rce.ShowWarning("Testing warning message on status bar");
    }
  }

  [CommandId("editor.testInfoMessage")]
  sealed class TestInfoMessageCommand : BaseTestCommand
  {
    public TestInfoMessageCommand()
    {
      Title = "Test Info Message";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      rce.ShowInfo("Testing info message on status bar");
    }
  }

  [CommandId("editor.testOkNotify")]
  sealed class TestOkNotifyCommand : BaseTestCommand
  {
    public TestOkNotifyCommand()
    {
      Title = "Test Ok Notify";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      rce.NotifyOk("Testing ok notify on status bar");
    }
  }

  [CommandId("editor.testErrorNotify")]
  sealed class TestErrorNotifyCommand : BaseTestCommand
  {
    public TestErrorNotifyCommand()
    {
      Title = "Test Error Notify";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      RhinoCode.Logger.Error("Testing error message in logger");
      rce.NotifyError("Testing error notify on status bar");
    }
  }

  [CommandId("editor.testWarningNotify")]
  sealed class TestWarningNotifyCommand : BaseTestCommand
  {
    public TestWarningNotifyCommand()
    {
      Title = "Test Warning Notify";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      RhinoCode.Logger.Warn("Testing warning message in logger");
      rce.NotifyWarning("Testing warning notify on status bar");
    }
  }

  [CommandId("editor.testInfoNotify")]
  sealed class TestInfoNotifyCommand : BaseTestCommand
  {
    public TestInfoNotifyCommand()
    {
      Title = "Test Info Notify";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      rce.NotifyInfo("Testing info notify on status bar");
    }
  }

  [CommandId("editor.testLocalize")]
  sealed class TestLocalizeCommand : BaseTestCommand
  {
    public TestLocalizeCommand()
    {
      Title = "Test Localization";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      rce.NotifyInfo(Localization.LocalizeString("Testing info notify on status bar", 1));
    }
  }

  [CommandId("editor.testSearch")]
  sealed class TestFindCommand : BaseTestCommand
  {
    public TestFindCommand()
    {
      Title = "Test Search";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      if (rce.ActiveState.ActiveCode is IStateCode activeStateCode)
        rce.Codes.Editors.Find(activeStateCode.Code, new TextSearchCriteria("import", regex: true));
    }
  }

  [CommandId("editor.testSetTitle")]
  sealed class TestSetTitleCommand : BaseTestCommand
  {
    public TestSetTitleCommand()
    {
      Title = "Test Set Title";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      if (rce.ActiveState.ActiveCode?.Code is Code code)
      {
        code.Title = "Test Title";
      }
    }
  }

  [CommandId("editor.testSetText")]
  sealed class TestSetTextCommand : BaseTestCommand
  {
    public TestSetTextCommand()
    {
      Title = "Test Set Text";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      if (rce.ActiveState.ActiveCode?.Code is Code code)
      {
        code.Text.Set(code.Text + code.Text);
        code.Text.Changed += OnTextChanged;
      }
    }

    void OnTextChanged(Code code, Code.TextChangedArgs _)
    {
      code.Text.Changed -= OnTextChanged;
      RhinoCode.Logger.Info(code.Text);
    }
  }

  [CommandId("editor.testToggleBreak")]
  sealed class TestToggleBreakCommand : BaseTestCommand
  {
    bool _lastState = true;

    public TestToggleBreakCommand()
    {
      Title = "Test Toggle Break";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      if (rce.Codes.Editors.ActiveEditContext is ICodeEditContext<Control> editCtx)
      {
        if (_lastState)
          editCtx.ClearBreak(5);
        else
          editCtx.Break(5);

        _lastState = !_lastState;
      }
    }
  }
}
