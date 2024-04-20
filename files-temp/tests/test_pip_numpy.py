"""Testing pip install specific packages"""

#! python 3
# r: numpy==1.24.2

import numpy

# print(numpy)
print(f"using numpy: {numpy.version.full_version}\n")

for i in numpy.random.rand(10):
    print(i)
