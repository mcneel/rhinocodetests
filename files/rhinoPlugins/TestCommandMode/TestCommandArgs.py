#! python3
from System.IO import BinaryWriter
from System.IO.MemoryMappedFiles import MemoryMappedFile
from System.Text import Encoding

mmf = MemoryMappedFile.OpenExisting("TestCommandArgsPy3")
stream = mmf.CreateViewStream()
writer = BinaryWriter(stream)
writer.Write(Encoding.UTF8.GetBytes(f"{__rhino_command__}\n"))
writer.Write(Encoding.UTF8.GetBytes(f"{__rhino_doc__}\n"))
writer.Write(Encoding.UTF8.GetBytes(f"{__rhino_runmode__}\n"))
writer.Write(Encoding.UTF8.GetBytes(f"{__is_interactive__}\n"))
stream.Dispose()
mmf.Dispose()