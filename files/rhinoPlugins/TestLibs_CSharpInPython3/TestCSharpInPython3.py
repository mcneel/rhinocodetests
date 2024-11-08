#! python3
from System.IO import BinaryWriter
from System.IO.MemoryMappedFiles import MemoryMappedFile
from System.Text import Encoding

from Test.CSharpInPython3 import TestClass

print(str(TestClass()))

mmf = MemoryMappedFile.OpenExisting("TestCSharpInPython3")
stream = mmf.CreateViewStream()
writer = BinaryWriter(stream)
writer.Write(Encoding.UTF8.GetBytes(str(TestClass()))) # 'Test.CSharpInPython3.TestClass'
stream.Dispose()
mmf.Dispose()
