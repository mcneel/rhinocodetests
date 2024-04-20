#! python3

from System.Collections.Generic import List

ints = List[int]([1,2,3])
# pythonnet.marshallInputs.ToPyList needs to be set in
# exec context for this to work. Grasshopper sets this option
# print(ints[2:3])

# print(ints.Skip(2).Take(1))

print(list(ints)[2:3])
