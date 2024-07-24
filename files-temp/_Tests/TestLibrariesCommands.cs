#pragma warning disable CA1822, IDE0060, IDE0270
using System;
using System.IO;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Storage;
using Rhino.Runtime.Code.Storage.Local;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Serialization.Json;

using RhinoCodeEditor.Attributes;

namespace RhinoCodeEditor.Editor.Commands._Tests
{
  [CommandId("editor.testCodeLibrary")]
  class TestCodeLibrarySerializationCommand : BaseTestCommand
  {
    public TestCodeLibrarySerializationCommand()
    {
      Title = "Test Code Library Serialization";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      ILanguage python = RhinoCode.Languages.QueryLatest(LanguageSpec.Python);
      if (python is null)
        throw new Exception($"Error finding {LanguageSpec.Python}");

      ILanguageLibrary A = TestPythonDirectory(python);
      ILanguageLibrary B = TestPythonSource(python);

      string aser = RhinoCodeJson.Serialize(A);
      ILanguageLibrary AD = RhinoCodeJson.Deserialize<ILanguageLibrary>(aser);

      string bser = RhinoCodeJson.Serialize(B);
      ILanguageLibrary BD = RhinoCodeJson.Deserialize<ILanguageLibrary>(bser);

      RhinoCode.Logger.Info(aser);
      //RhinoCode.Logger.Info(bser);
      RhinoCode.Logger.Info($"[{A.Signature == AD.Signature}] (de)serializing library");
      RhinoCode.Logger.Info($"[{B.Signature == BD.Signature}] (de)serializing source library");
    }

    ILanguageLibrary TestPythonDirectory(ILanguage python)
    {
      string testPath = Path.Combine(RhinoCode.Directory, "tests");
      testPath.EnsureDirectory();
      string testLibPath = Path.Combine(testPath, "testmodule");
      testLibPath.EnsureDirectory();
      string libcode = Path.Combine(testLibPath, "__init__.py");
      File.WriteAllText(libcode, "from math import *");
      libcode = Path.Combine(testLibPath, "math.py");
      File.WriteAllText(libcode, @""""""" math library""""""
def add(x, y):
  return x + y

");

      // serializing library generically
      return python.CreateLibrary(testLibPath.ToUri());
    }

    ILanguageLibrary TestPythonSource(ILanguage python)
    {
      LanguageSourceLibrary module = python.CreateLibrary("testsourcemodule");

      module.Add(new SourceCode("__init__.py", "from math import *"));
      module.Add(new SourceCode("math.py", @""""""" math library""""""
def add(x, y):
  return x + y

"));

      return module;
    }
  }

  [CommandId("editor.testCodeLibrary")]
  class TestCodeLibraryCommand : BaseTestCommand
  {
    readonly BuildOptions _buildctx = new BuildOptions(BuildKind.Run);

    public TestCodeLibraryCommand()
    {
      Title = "Test Code Library";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      ILanguage python = RhinoCode.Languages.QueryLatest(LanguageSpec.Python);
      if (python is null)
        throw new Exception($"Error finding {LanguageSpec.Python}");

      ILanguage csharp = RhinoCode.Languages.QueryLatest(LanguageSpec.CSharp);
      if (csharp is null)
        throw new Exception($"Error finding {LanguageSpec.CSharp}");

      bool A = TestPythonDirectory(python) == 42;
      bool B = TestPythonSource(python) == 42;
      bool C = TestCSharpDirectory(csharp) == 42;
      bool D = TestCSharpSource(csharp) == 42;
      bool E = TestPythonFromDotNet(csharp) == 42;
      RhinoCode.Logger.Info($"[{A}] Test python module from directory");
      RhinoCode.Logger.Info($"[{B}] Test python module from source");
      RhinoCode.Logger.Info($"[{C}] Test csharp library from directory");
      RhinoCode.Logger.Info($"[{D}] Test csharp library from source");
      RhinoCode.Logger.Info($"[{E}] Test csharp library from source used in python");
    }

    int TestPythonDirectory(ILanguage python)
    {
      string testPath = Path.Combine(RhinoCode.Directory, "tests");
      testPath.EnsureDirectory();
      string testLibPath = Path.Combine(testPath, "testmodule");
      testLibPath.EnsureDirectory();
      string libcode = Path.Combine(testLibPath, "__init__.py");
      File.WriteAllText(libcode, "from math import *");
      libcode = Path.Combine(testLibPath, "math.py");
      File.WriteAllText(libcode, @""""""" math library""""""
def add(x, y):
  return x + y

");

      var module = python.CreateLibrary(testLibPath.ToUri());
      if (module.TryBuild(_buildctx, out CompileReference reference, out DiagnosticSet _))
      {
        RhinoCode.Logger.Info($"Deployed directory library @ {reference}");

        Code code = new SourceCode("#! python3\nfrom testmodule import math\nx = math.add(21, 21)\n").CreateCode();

        code.Outputs.Add("x");
        code.References.Add(reference);

        var ctx = new RunContext { Outputs = { ["x"] = 0 } };

        code.Run(ctx);

        if (ctx.Outputs.TryGet("x", out int value))
          return value;
        return 0;
      }
      else
        throw new Exception("Error deploying python module");
    }

    int TestPythonSource(ILanguage python)
    {
      LanguageSourceLibrary module = python.CreateLibrary("testsourcemodule");

      module.Add(new SourceCode("__init__.py", "from math import *"));
      module.Add(new SourceCode("math.py", @""""""" math library""""""
def add(x, y):
  return x + y

"));

      if (module.TryBuild(_buildctx, out CompileReference reference, out DiagnosticSet _))
      {
        RhinoCode.Logger.Info($"Deployed source library @ {reference}");

        Code code = new SourceCode("#! python3\nfrom testsourcemodule import math\nx = math.add(21, 21)\n").CreateCode();

        code.Outputs.Add("x");
        code.References.Add(reference);

        var ctx = new RunContext { Outputs = { ["x"] = 0 } };

        code.Run(ctx);

        if (ctx.Outputs.TryGet("x", out int value))
          return value;
        return 0;
      }
      else
        throw new Exception("Error deploying python source module");
    }

    int TestCSharpDirectory(ILanguage csharp)
    {
      string testPath = Path.Combine(RhinoCode.Directory, "tests");
      testPath.EnsureDirectory();
      string testLibPath = Path.Combine(testPath, "TestModule");
      testLibPath.EnsureDirectory();
      string libcode = Path.Combine(testLibPath, "Math.cs");
      File.WriteAllText(libcode, @"
using System;

namespace TestModule.Math
{
public class DoMath
{
public int Add(int x, int y) => x + y;
}
}
");

      ILanguageLibrary library = csharp.CreateLibrary(testLibPath.ToUri());
      if (library.TryBuild(_buildctx, out CompileReference reference, out DiagnosticSet _))
      {
        RhinoCode.Logger.Info($"Compiled directory library @ {reference}");

        Code code = new SourceCode(@"
// #! csharp
using TestModule;

var math = new TestModule.Math.DoMath();
x = math.Add(21, 21);
").CreateCode();

        code.Outputs.Add("x");
        code.References.Add(reference);

        var ctx = new RunContext { Outputs = { ["x"] = 0 } };

        code.Run(ctx);

        if (ctx.Outputs.TryGet("x", out int value))
          return value;
        return 0;
      }
      else
        throw new Exception("Error deploying C# module");
    }

    int TestCSharpSource(ILanguage csharp)
    {
      LanguageSourceLibrary library = csharp.CreateLibrary("TestSourceModule");

      library.Add(new SourceCode("Math.cs", @"
using System;

namespace TestSourceModule.Math
{
public class DoMath
{
public int Add(int x, int y) => x + y;
}
}
"));

      if (library.TryBuild(_buildctx, out CompileReference reference, out DiagnosticSet _))
      {
        RhinoCode.Logger.Info($"Compiled source library @ {reference}");

        Code code = new SourceCode(@"
// #! csharp
using TestSourceModule;

var math = new TestSourceModule.Math.DoMath();
x = math.Add(21, 21);
").CreateCode();

        code.Outputs.Add("x");
        code.References.Add(reference);

        var ctx = new RunContext { Outputs = { ["x"] = 0 } };

        code.Run(ctx);

        if (ctx.Outputs.TryGet("x", out int value))
          return value;
        return 0;
      }
      else
        throw new Exception("Error deploying C# source module");
    }

    int TestPythonFromDotNet(ILanguage csharp)
    {
      LanguageSourceLibrary library = csharp.CreateLibrary("TestSourceModule");

      library.Add(new SourceCode("Math.cs", @"
using System;

namespace TestSourceModule.Math
{
public class DoMath
{
public int Add(int x, int y) => x + y;
}
}
"));

      if (library.TryBuild(_buildctx, out CompileReference reference, out DiagnosticSet _))
      {
        RhinoCode.Logger.Info($"Deployed source library @ {reference}");

        Code code = new SourceCode(@"#! python3
from TestSourceModule import Math

math = Math.DoMath()

x = math.Add(21, 21)

").CreateCode();

        code.Outputs.Add("x");
        code.References.Add(reference);

        var ctx = new RunContext { Outputs = { ["x"] = 0 } };

        code.Run(ctx);

        if (ctx.Outputs.TryGet("x", out int value))
          return value;
        return 0;
      }
      else
        throw new Exception("Error deploying dotnet source module");
    }
  }
}
