#! python 2
import clr
clr.AddReference("System.IO.MemoryMappedFiles")
from System.IO import BinaryWriter
from System.IO.MemoryMappedFiles import MemoryMappedFile
from System.Text import Encoding

from Test.CSharpInPython2 import TestClass

print(str(TestClass()))

mmf = MemoryMappedFile.OpenExisting("TestCSharpInPython2")
stream = mmf.CreateViewStream()
writer = BinaryWriter(stream)
writer.Write(Encoding.UTF8.GetBytes(str(TestClass()))) # '<Test.CSharpInPython3.TestClass'
stream.Dispose()
mmf.Dispose()
