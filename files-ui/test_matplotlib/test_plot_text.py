#! python 3
import matplotlib
import matplotlib.pyplot as plt

plt.plot([0, 1], [0, 1])
plt.annotate("Annotation", xy=(0.5, 0.5), xytext=(0.2, 0.8),
             arrowprops=dict(facecolor='black', shrink=0.05))
plt.title("Annotation Test")
plt.show()