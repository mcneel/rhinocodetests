#! python 3
import matplotlib
import matplotlib.pyplot as plt

fig, axs = plt.subplots(2, 2)
for i, ax in enumerate(axs.flat):
    ax.plot([1, 2, 3], [j * (i + 1) for j in [1, 2, 3]])
    ax.set_title(f"Plot {i+1}")
plt.tight_layout()
plt.show()