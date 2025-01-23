#! python 3

from System.Collections.Generic import List

ints = List[int]([1,2,3])
assert list(ints)[2:3] == [3]

result = True