#! python 3
## flag: python.resetEngine

import somelib
import someotherlib

print(somelib.__spec__)
print(someotherlib.__spec__)

print(__name__)
print(somelib.foo())
print(someotherlib.foo())