#! python 3
import os.path as op
import matplotlib
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
import numpy as np


d = op.dirname(__file__)
f = op.join(d, "test_output.png")

plt.plot([1, 2, 3], [1, 4, 9])
plt.title("Save to File")
plt.savefig(f)