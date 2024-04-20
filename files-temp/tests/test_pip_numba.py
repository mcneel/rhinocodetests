#! python 3
# https://numba.pydata.org
# r: numpy, numba
import numpy as np
from numba import njit, jit
import random


# jit compiled and not debuggable
@njit
def monte_carlo_pi(nsamples):
    acc = 0
    for i in range(nsamples):
        x = random.random()
        y = random.random()
        if (x ** 2 + y ** 2) < 1.0:
            acc += 1
    return 4.0 * acc / nsamples


# Numba Specialization by Dtype
@jit(nopython=True)
def zero_clamp(x, threshold):
    # assume 1D array
    out = np.empty_like(x)
    for i in range(out.shape[0]):
        if np.abs(x[i]) > threshold:
            out[i] = x[i]
        else:
            out[i] = 0
    return out


print(monte_carlo_pi(10))

a_small = np.linspace(0, 1, 50)
print(zero_clamp(a_small, 0.3))
