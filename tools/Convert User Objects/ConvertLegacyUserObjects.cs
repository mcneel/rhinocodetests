// #! csharp
#r "Grasshopper.dll"
#r "GH_IO.dll"
#r "RhinoCodePlatform.GH.dll"
#r "RhinoCodePlatform.GH.Context.dll"
using System;
using System.IO;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.UI;
using GH_IO;
using GH_IO.Serialization;
using Eto.Drawing;
using Eto.Forms;
using RhinoCodePlatform.GH;
using RhinoCodePlatform.GH.Context;

// legacy ids
Guid s_legacyIPy2 = new Guid("410755b1-224a-4c1e-a407-bf32fb45ea7e");
Guid s_legacyCS = new Guid("a9a8ebd2-fff5-4c44-a8f5-739736d129ba");
Guid s_legacyVB = new Guid("079bd9bd-54a0-41d4-98af-db999015f63d");
Guid s_legacyIPy2_ghdocObjHint = new Guid("87F87F55-5B71-41F4-8AEA-21D494016F81");

// new ids
Guid s_newPy3 = new Guid("719467e6-7cf5-4848-99b0-c5dd57e5442c");
Guid s_newIPy2 = new Guid("97aa26ef-88ae-4ba6-98a6-ed6ddeca11d1");
Guid s_newPy_ghdocObjHint = new Guid("1c282eeb-dd16-439f-94e4-7d92b542fe8b");
Guid s_newCS = new Guid("b6ba1144-02d6-4a2d-b53c-ec62e290eeb7");

bool TryConvertLegacyUO(string userObjFile, string destPath = default, string nameFormat = ConverterOptions.DEFAULT_NAME_FORMAT)
{
    if (File.Exists(userObjFile)
            && Path.GetExtension(userObjFile) == ".ghuser")
    {
        string name = Path.GetFileNameWithoutExtension(userObjFile);
        string newName = string.Format(nameFormat, name);
        string path = Path.GetDirectoryName(userObjFile);
        destPath = destPath ?? path;
        string newPath = Path.Combine(destPath, newName);

        var uo = new GH_Archive();
        uo.Deserialize_Binary(File.ReadAllBytes(userObjFile));

        GH_Chunk chunk = uo.GetRootNode;
        if (chunk.Name == "UserObject")
        {
            var id = chunk.GetGuid("BaseID");
            Guid newId = Guid.Empty;

            if (s_newPy3 == id)                           newId = s_newPy3;
            if (s_legacyIPy2 == id || s_newIPy2 == id)    newId = s_newIPy2;
            if (s_legacyCS == id   || s_newCS == id)      newId = s_newCS;

            if (newId != Guid.Empty)
            {
                chunk.RemoveItem("BaseID");
                chunk.SetGuid("BaseID", newId);

                var so = new GH_Archive();
                so.Deserialize_Binary(chunk.GetByteArray("Object"));
                GH_Chunk soc = so.GetRootNode;
                
                // File.WriteAllText(Path.Combine(destPath, newName + ".xml"), uo.Serialize_Xml());
                // File.WriteAllText(Path.Combine(destPath, newName + ".script.xml"), so.Serialize_Xml());

                if (Grasshopper.Instances.ComponentServer.EmitObject(newId) is IGH_Component comp)
                {
                    try
                    {
                        IScriptObject sc = (IScriptObject)comp;

                        sc.Text = GetScriptSource(soc);

                        if (TryGetHasExternScript(soc, out bool hasExternScript))
                        {
                            sc.HasExternScript = hasExternScript;
                            // if (sc.HasExternScript
                            //         && comp.Params.Input[0] is RhinoCodePluginGH.Parameters.ScriptParam scriptInput)
                            // {
                            //     scriptInput.TreatInputAsPath = soc.GetBoolean("InputIsPath");
                            // }
                        }
                        else
                            hasExternScript = sc.HasExternScript;

                        if (TryGetCapturingOutput(soc, out bool captureOutput))
                        {
                            sc.CaptureOutput = captureOutput;
                        }
                        else
                            captureOutput = sc.CaptureOutput;

                        if (TryGetMarshGuids(soc, out bool marshGuids))
                        {
                            sc.MarshGuids = marshGuids;
                        }

                        var pd = soc.FindChunk("ParameterData");
                        int start = hasExternScript ? 1 : 0;
                        for(int i = start; i < pd.GetInt32("InputCount"); i++)
                        {
                            GH_Chunk p = (GH_Chunk)pd.FindChunk("InputParam", i);
                            if (p.ItemExists("TypeHintID"))
                            {
                                Guid hint = p.GetGuid("TypeHintID");
                                if (s_legacyIPy2_ghdocObjHint == hint){
                                    p.RemoveItem("TypeHintID");
                                    p.SetGuid("TypeHintID", s_newPy_ghdocObjHint);
                                }
                            }
                            
                            dynamic inp = comp.Params.Input[i];
                            
                            inp.Read(p);
                        }
                        start = captureOutput ? 1 : 0;
                        for(int i = start; i < pd.GetInt32("OutputCount"); i++)
                        {
                            var p = pd.FindChunk("OutputParam", i);
                            dynamic outp = comp.Params.Output[i];

                            outp.Read(p);
                        }

                        sc.ParamsApply();

                        var doco = new GH_Archive();
                        doco.CreateNewRoot(forWriting: true);
                        comp.Write(doco.GetRootNode);

                        chunk.RemoveItem("Object");
                        chunk.SetByteArray("Object", doco.Serialize_Binary());

                        File.WriteAllBytes(newPath, uo.Serialize_Binary());

                        Console.WriteLine($"Successfully converted: {name}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Skipped conversion. Unknown archive format: {name} | {ex}");
                    }
                }
                else
                    Console.WriteLine($"Skipped conversion. Failed to find new script component: {name}");
            }
            else
                Console.WriteLine($"Skipped conversion. Unsupported scripting language: {name}");
        }
    }

    return false;
}

string CSTemplate = @"#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion

    private void RunScript(object x, object y, out object a)
    {
[SCRIPT]
    }
}
";

string GetScriptSource(GH_Chunk chunk)
{
    // Legacy IronPython
    if (chunk.ItemExists("CodeInput"))
    {
        string source = chunk.GetString("CodeInput");
        source = source.Replace("from ghpythonlib.componentbase import executingcomponent as component", "");
        source = source.Replace("import Grasshopper, GhPython", "import Grasshopper");
        source = source.Replace("(component)", "(Grasshopper.Kernel.GH_ScriptInstance)");
        return source;
    }
    
    // Legacy C#
    else if (chunk.ItemExists("ScriptSource"))
    {
        string source = chunk.GetString("ScriptSource");
        return CSTemplate.Replace("[SCRIPT]", source);
    }

    return string.Empty;
}

bool TryGetHasExternScript(GH_Chunk chunk, out bool hasExternScript)
{
    hasExternScript = default;

    // Legacy IronPython
    if (chunk.ItemExists("HideInput"))
    {
        hasExternScript = !chunk.GetBoolean("HideInput");
        return true;
    }
    
    return false;
}

bool TryGetCapturingOutput(GH_Chunk chunk, out bool captureOutput)
{
    captureOutput = default;

    // Legacy IronPython
    if (chunk.ItemExists("HideOutput"))
    {
        captureOutput = !chunk.GetBoolean("HideOutput");
        return true;        
    }
    
    // Legacy C#
    else if (chunk.ItemExists("OutParameter"))
    {
        captureOutput = chunk.GetBoolean("OutParameter");
        return true;        
    }

    return false;
}

bool TryGetMarshGuids(GH_Chunk chunk, out bool marshGuids)
{
    marshGuids = default;

    // Legacy IronPython
    if (chunk.ItemExists("MarshalOutGuids"))
    {
        marshGuids = chunk.GetBoolean("MarshalOutGuids");
        return true;
    }
    
    return false;
}

class ConverterOptions : Dialog
{
    public const string DEFAULT_NAME_FORMAT = "{0}_converted.ghuser";

    readonly Button _okButton = new Button { Text = "Convert" };
    readonly Button _cancelButton = new Button { Text = "Cancel" };
    readonly TextBox _destPath = new();
    readonly TextBox _nameFormat = new();
    
    public string DestinationPath => Path.GetFullPath(_destPath.Text);

    public string NameFormat => _nameFormat.Text;

    public bool IsCancelled { get; private set; } = false;

    public ConverterOptions(string destPath)
    {
        if (string.IsNullOrWhiteSpace(destPath))
            throw new ArgumentNullException(nameof(destPath));

        _destPath.Text = destPath;
        _nameFormat.Text = DEFAULT_NAME_FORMAT;

        Content = new TableLayout {
            Padding = new Padding(10, 10),
            Spacing = new Size(0, 5),
            Rows = {
                new TableRow { Cells = { new Label { Text = "Destination Path:" }} },
                new TableRow { Cells = { _destPath } },
                new TableRow { Cells = { new Label { Text = "Name Format (original name replaces \"{0}\")" }} },
                new TableRow { Cells = { _nameFormat } },
                new TableRow { Cells = { _cancelButton } },
                new TableRow { Cells = { _okButton } },
            }
        };

        _okButton.Click += (s, e) => Close();
        _cancelButton.Click += (s, e) => {
            IsCancelled = true;
            Close();
        };

        Title = "Convert Options";
        MinimumSize = new Size(450, 200);
        DefaultButton = _okButton;
    }
}

var sd = new Eto.Forms.OpenFileDialog {
    MultiSelect = true,
    CurrentFilter = new FileFilter("*.ghuser"),
};

if (sd.ShowDialog(RhinoEtoApp.MainWindowForOwner) == Eto.Forms.DialogResult.Ok
        && sd.Filenames.Any())
{
    var opt = new ConverterOptions(Path.GetDirectoryName(sd.Filenames.First()));
    opt.ShowModal();

    if (!opt.IsCancelled)
        foreach (string file in sd.Filenames)
        {
            TryConvertLegacyUO(file, destPath: opt.DestinationPath, nameFormat: opt.NameFormat);
        }
}
