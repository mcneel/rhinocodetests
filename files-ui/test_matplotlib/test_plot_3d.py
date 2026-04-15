#! python 3
import matplotlib
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
import numpy as np

fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')
x, y = np.meshgrid(range(10), range(10))
z = np.sin(x) + np.cos(y)
ax.plot_surface(x, y, z, cmap='plasma')
plt.title("3D Surface Plot")
plt.show()