#! python3

import System
from System.Collections import IEnumerable, IEnumerator


class CustomIterator(IEnumerator):
    def __init__(self, enumerable):
        self.enumerable = enumerable
        self.position = -1

    def get_Current(self):
        if self.position == -1: return None
        if self.position >= len(self.enumerable): return None
        return self.enumerable[self.position]

    def MoveNext(self):
        self.position += 1
        return self.position < len(self.enumerable)

    def Reset(self):
        self.position = -1


class CustomEnumerable(IEnumerable):
    def __init__(self, values):
        self.values = values
        self.myattributes = ['a', 'b']

    def __getitem__(self, key):
        return self.values[key]

    def __len__(self):
        return len(self.values)

    def __iter__(self):
       for a in self.myattributes:
          yield a

    def GetEnumerator(self):
        return CustomIterator(self)



e = CustomEnumerable([1, 2, 3])

print(len(e))
print(e[0])
print([x for x in e])