#! python 2
import clr
clr.AddReference("System.IO.MemoryMappedFiles")
from System.IO import BinaryWriter
from System.IO.MemoryMappedFiles import MemoryMappedFile
from System.Text import Encoding

from TestCSharpInPython2 import TestClass

print(str(TestClass()))

mmf = MemoryMappedFile.OpenExisting("TestCSPy2")
stream = mmf.CreateViewStream()
writer = BinaryWriter(stream)
writer.Write(Encoding.UTF8.GetBytes(str(TestClass()))) # '<TestCSharpInPython2.TestClass'
stream.Dispose()
mmf.Dispose()
