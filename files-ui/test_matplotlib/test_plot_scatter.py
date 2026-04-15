#! python 3
import matplotlib.pyplot as plt
import numpy as np

x = np.random.rand(100)
y = np.random.rand(100)

plt.figure()
plt.scatter(x, y, alpha=0.7)
plt.title("Scatter Plot")
plt.xlabel("X")
plt.ylabel("Y")
plt.show()