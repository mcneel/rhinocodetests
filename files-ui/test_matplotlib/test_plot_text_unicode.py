#! python 3
import matplotlib
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
import numpy as np

plt.text(0.5, 0.5, "Unicode test: 你好 🌍", fontsize=14, ha='center')
plt.axis('off')
plt.title("Unicode & Font Rendering")
plt.show()