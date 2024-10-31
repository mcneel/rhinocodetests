#pragma warning disable CA1822, IDE0060
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Editing;
using Rhino.Runtime.Code.Storage;
using Rhino.Runtime.Code.Storage.Local;
using Rhino.Runtime.Code.Serialization;
using Rhino.Runtime.Code.Serialization.Json;
using Rhino.Runtime.Code.Platform;
using D = Rhino.Runtime.Code.Display;

using RhinoCodeEditor.Attributes;

namespace RhinoCodeEditor.Editor.Commands
{
  abstract class TestTemplatesCommand : BaseTestCommand
  {
    public class TestTemplate : Template
    {
      public static TestTemplate Empty { get; } = new TestTemplate("No/Empty Storage", string.Empty);

      public TestTemplate() { }

      public TestTemplate(string title, string text)
      {
        Title = title;
        Text = text ?? string.Empty;

        SetLanguageSpec();
      }

      public TestTemplate(string title, Uri uri)
      {
        Title = title;

        if (uri is null)
          throw new ArgumentNullException(nameof(uri));

        IStorage storage = uri.GetStorage();
        Uri = uri;

        SetText(storage);
        SetLanguageSpec();
      }

      public override Code CreateCode() => GetLanguage().CreateCode(Text);

      public override void Read(IReader reader)
      {
        ReadTitle(reader);
        ReadText(reader);
        ReadUri(reader);
      }

      public override void Write(IWriter writer)
      {
        WriteTitle(writer);
        WriteText(writer);
        WriteUri(writer);
      }
    }

    public class TestTemplateLibrary : Library<TestTemplate>
    {
      [DataConstructor]
      public TestTemplateLibrary() { }

      public TestTemplateLibrary(Uri baseUri) : base(baseUri) { }

      public void Add(TestTemplate template) => base.LockAndAdd(template);

      public void ChangeBase(Uri baseUri) => Base = baseUri;
    }
  }

  [CommandId("editor.testSerialization")]
  class TestSerializationCommand : TestTemplatesCommand
  {
    public TestSerializationCommand()
    {
      Title = "Test Serialization";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      // test color and image  ------------------------------------------------
      var color = new D.Color(255, 0, 0, 255);
      string colorS = RhinoCodeJson.Serialize(color);
      RhinoCode.Logger.Info(colorS);

      D.Color colorD = RhinoCodeJson.Deserialize<D.Color>(colorS);
      RhinoCode.Logger.Info(colorD.ToString());

      var image = new D.Bitmap(new D.Color[] { new D.Color(255, 0, 0, 255) }, 1);
      string imageS = RhinoCodeJson.Serialize(image);
      RhinoCode.Logger.Info(imageS);

      D.Bitmap imageD = RhinoCodeJson.Deserialize<D.Bitmap>(imageS);
      RhinoCode.Logger.Info(imageD.ToString());


      // test enum ------------------------------------------------------------
      EditorTheme themes = new EditorTheme
      {
        Appearance = D.Appearance.Dark
      };

      string optsS = RhinoCodeJson.Serialize(themes);
      RhinoCode.Logger.Info(optsS);

      EditorTheme themeD = RhinoCodeJson.Deserialize<EditorTheme>(optsS);
      RhinoCode.Logger.Info(themeD.ToString());


      // test language id -----------------------------------------------------
      var langSpec = new LanguageSpec("mcneel.test.lang", new Version(10, 9));

      string langSpecS = RhinoCodeJson.Serialize(langSpec);
      RhinoCode.Logger.Info(langSpecS);

      LanguageSpec langSpecD = RhinoCodeJson.Deserialize<LanguageSpec>(langSpecS);
      RhinoCode.Logger.Info(langSpecD.ToString());

      // test template --------------------------------------------------------
      var template = new TestTemplate("file.a", "file.a contents");

      string templateS = RhinoCodeJson.Serialize(template);
      RhinoCode.Logger.Info(templateS);

      TestTemplate templateD = RhinoCodeJson.Deserialize<TestTemplate>(templateS);
      RhinoCode.Logger.Info(templateD.ToString());

      // test list of editor opts  --------------------------------------------
      var editorThemes = new Dictionary<string, EditorTheme>
      {
        ["one"] = themes,
        ["two"] = themes
      };

      string editorThemesS = RhinoCodeJson.Serialize(editorThemes);
      RhinoCode.Logger.Info(editorThemesS);

      Dictionary<string, EditorTheme> editorThemesD = RhinoCodeJson.Deserialize<Dictionary<string, EditorTheme>>(editorThemesS);
      RhinoCode.Logger.Info(editorThemesD.ToString());

      // test list of templates -----------------------------------------------
      var templates = new List<TestTemplate>
      {
        new TestTemplate("file.a", "file.a contents")
    };

      string templatesS = RhinoCodeJson.Serialize(templates);
      RhinoCode.Logger.Info(templatesS);

      List<TestTemplate> templatesD = RhinoCodeJson.Deserialize<List<TestTemplate>>(templatesS);
      RhinoCode.Logger.Info(templatesD.ToString());
    }
  }

  [CommandId("editor.testLibrary")]
  class TestLibraryCommand : TestTemplatesCommand
  {
    public TestLibraryCommand()
    {
      Title = "Test Library";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      /*NOTE: library design
      > Library paths might be relative but that's to the current library base
         and it does not reflect relativity in storage uris. Do not check the
         absoluteness of a library path to determine anything beyond the fact that
         the path is relative under the library base
      > Primary method of adding codes should be by Add(x) WITHOUT specifying a
        hard path. this way changing library base will dynamically change the paths
      > When changing base, the # of paths might change
      > Hard paths specified by Add(uri, x) could be returned as relative paths
        if the path is relative to the library base
      */

      string testPath = Path.Combine(RhinoCode.Directory, "tests");
      testPath.EnsureDirectory();
      string testLibPath = Path.Combine(testPath, "lib");
      testLibPath.EnsureDirectory();
      string filea = Path.Combine(testPath, "file.a");
      File.WriteAllText(filea, "#! python3\nimport os");
      string fileb = Path.Combine(testLibPath, "file.b");
      File.WriteAllText(fileb, "#! python3\nimport os");

      string libPath = Path.Combine(testPath, "library");
      libPath.EnsureDirectory();
      string libInnerPath = Path.Combine(libPath, "inner");
      libInnerPath.EnsureDirectory();
      string filec = Path.Combine(libPath, "file.c");
      File.WriteAllText(filec, "#! python3\nimport os");
      string filed = Path.Combine(libInnerPath, "file.d");
      File.WriteAllText(filed, "#! python3\nimport os");

      // create lib
      Uri baseUri = new Uri(libPath);
      var lib = new TestTemplateLibrary(baseUri)
      {
        TestTemplate.Empty,
        new TestTemplate("file.a (A)", new Uri(filea)),
        new TestTemplate("file.b (B)", new Uri(fileb)),
        new TestTemplate("file.c (C)", new Uri(filec)),
        new TestTemplate("file.d (D)", new Uri(filed)),
      };

      Log(lib);

      // change base and test
      lib.ChangeBase(new Uri(Path.Combine(RhinoCode.Directory, "misc")));
      Log(lib);

      // test IO
      string libStr = RhinoCodeJson.Serialize(lib);
      TestTemplateLibrary readlib = RhinoCodeJson.Deserialize<TestTemplateLibrary>(libStr);
      LogLibrary(readlib);
      string readLibStr = RhinoCodeJson.Serialize(readlib);
      RhinoCode.Logger.Info($"IO Write/Read Test: {libStr.Equals(readLibStr)}");

      Directory.Delete(testPath, recursive: true);
    }

    public static void Log(ILibrary lib)
    {
      LogLibrary(lib);
      RhinoCode.Logger.Info(RhinoCodeJson.Serialize(lib));
    }

    public static void LogLibrary(ILibrary lib)
    {
      var log = new StringBuilder();
      log.AppendLine("-");

      foreach (ILibraryPath path in lib.Paths)
        log.AppendLine($"-> {path}");

      RhinoCode.Logger.Info(log.ToString());
    }
  }

  [CommandId("editor.testProject")]
  class TestProjectCommand : TestTemplatesCommand
  {
    public TestProjectCommand()
    {
      Title = "Test Project";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      string testPath = Path.Combine(RhinoCode.Directory, "tests");
      testPath.EnsureDirectory();
      string testLibPath = Path.Combine(testPath, "lib");
      testLibPath.EnsureDirectory();
      string commanda = Path.Combine(testPath, "command.a");
      File.WriteAllText(commanda, "#! python3\nimport os");
      string codea = Path.Combine(testLibPath, "code.a");
      File.WriteAllText(codea, "#! python3\nimport os");

      var project = RhinoCode.Platforms.First().CreateProject();
      //project.AppendCommand(new ProjectCommand(new Uri(commanda)));
      //project.AppendLibraryCode(new ProjectCode(new Uri(codea)));

      TestLibraryCommand.Log(project);
    }
  }

  [CommandId("editor.testActiveProject")]
  class TestActiveProjectCommand : TestTemplatesCommand
  {
    public TestActiveProjectCommand()
    {
      Title = "Test Active Project";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      if (rce.ActiveState.ActiveProject?.Project is IProject project)
        TestLibraryCommand.Log(project);
    }
  }

  [CommandId("editor.testLanguageSpecFromPath")]
  class TestLanguageSpecFromPath : TestTemplatesCommand
  {
    public TestLanguageSpecFromPath()
    {
      Title = "Test Toggle Breakpoint";
      Description = Title;
    }

    public override void ExecuteWith(RCE rce)
    {
      var e = new StorageEntry(new Uri(@"file:///Users/ein/gits/McNeel/rhinocodeplugins/tests/test_csharp.cs"));
      RhinoCode.Logger.Info($"lang spec: {e.LanguageSpec}");
    }
  }
}
