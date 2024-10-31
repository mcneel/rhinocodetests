# r "..\tests_projects\libraries\TestAssembly\bin\TestAssembly.dll"

import clr

clr.AddReference("TestAssembly")
from TestAssembly import Math

math = Math.DoMath()

print(f"Result: {math.Add(21, 21)}")
