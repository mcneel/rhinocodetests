#! python3
from Rhino.Input.Custom import GetString
from Rhino.Input import GetResult
from Test.CSharpInPython3 import TestClass

g = GetString()
g.SetCommandPrompt("GetFile")
res = g.Get()
if res == GetResult.String:
    path = g.StringResult()
    with open(path, 'w') as f:
        f.write(str(TestClass())) # 'Test.CSharpInPython3.TestClass'
