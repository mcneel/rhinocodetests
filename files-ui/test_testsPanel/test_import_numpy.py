#! python 3
import numpy

randoms = list(numpy.random.rand(10))

result = len(randoms) > 0 \
     and r'site-envs\\default-' in str(numpy)
