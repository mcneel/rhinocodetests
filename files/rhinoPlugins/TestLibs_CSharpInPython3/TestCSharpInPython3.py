#! python3
from System.IO import BinaryWriter
from System.IO.MemoryMappedFiles import MemoryMappedFile
from System.Text import Encoding

from TestCSharpInPython3 import TestClass

print(str(TestClass()))

mmf = MemoryMappedFile.OpenExisting("TestCSPy3")
stream = mmf.CreateViewStream()
writer = BinaryWriter(stream)
writer.Write(Encoding.UTF8.GetBytes(str(TestClass()))) # 'TestCSharpInPython3.TestClass'
stream.Dispose()
mmf.Dispose()
