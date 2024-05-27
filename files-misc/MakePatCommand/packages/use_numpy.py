"""Testing pip install specific packages"""
# r: numpy

import numpy

print(f"using numpy: {numpy.version.full_version}\n")

for i in numpy.random.rand(10):
    print(i)
