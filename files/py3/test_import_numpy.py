#! python 3
import numpy

randoms = list(numpy.random.rand(10))

result = len(randoms) > 0 \
     and 'site-envs' in str(numpy)