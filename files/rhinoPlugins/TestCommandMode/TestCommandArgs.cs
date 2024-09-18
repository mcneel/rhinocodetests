// #! csharp
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

using MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("TestCommandArgsCS");
using MemoryMappedViewStream stream = mmf.CreateViewStream();
BinaryWriter writer = new BinaryWriter(stream);
writer.Write(Encoding.UTF8.GetBytes($"{__rhino_command__}\n"));
writer.Write(Encoding.UTF8.GetBytes($"{__rhino_doc__}\n"));
writer.Write(Encoding.UTF8.GetBytes($"{__rhino_runmode__}\n"));
writer.Write(Encoding.UTF8.GetBytes($"{__is_interactive__}\n"));