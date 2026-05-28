#! python 3
# requirements: numpy
import numpy

randoms = list(numpy.random.rand(10))

result = len(randoms) > 0 \
     and r'site-envs\\' in str(numpy)
