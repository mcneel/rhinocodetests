#! python 3
import matplotlib
import matplotlib.pyplot as plt
import numpy as np

img = np.random.rand(10, 10)
plt.imshow(img, cmap='viridis', interpolation='nearest')
plt.colorbar()
plt.title("Imshow Test")
plt.show()