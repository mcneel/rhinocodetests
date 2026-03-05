# r: matplotlib
import matplotlib
import matplotlib.pyplot as plt
import numpy as np
import matplotlib.dates as mdates
from datetime import datetime, timedelta

# Generate some dates
dates = [datetime.today() - timedelta(days=i) for i in range(30)][::-1]
x = np.array(dates)
y1 = np.sin(np.linspace(0, 3 * np.pi, 30)) * 100
y2 = np.random.uniform(0.5, 1.5, size=30).cumsum()

fig, ax1 = plt.subplots(figsize=(12, 6))

# Plot y1 on ax1
ax1.plot(x, y1, "b-", label="Sine Wave")
ax1.set_ylabel("Sine Value", color="b")
ax1.tick_params(axis="y", labelcolor="b")

# Create a twin y-axis for y2
ax2 = ax1.twinx()
ax2.plot(x, y2, "r--", label="Cumulative Random", alpha=0.6)
ax2.set_ylabel("Cumulative Value", color="r")
ax2.tick_params(axis="y", labelcolor="r")

# Formatting x-axis dates
ax1.xaxis.set_major_locator(mdates.WeekdayLocator())
ax1.xaxis.set_major_formatter(mdates.DateFormatter("%b %d"))

# Add annotation
max_idx = np.argmax(y2)
ax2.annotate(
    "Peak",
    xy=(x[max_idx], y2[max_idx]),
    xytext=(x[max_idx], y2[max_idx] + 2),
    arrowprops=dict(facecolor="black", arrowstyle="->"),
    fontsize=10,
)

# Add legends
lines1, labels1 = ax1.get_legend_handles_labels()
lines2, labels2 = ax2.get_legend_handles_labels()
ax1.legend(lines1 + lines2, labels1 + labels2, loc="upper left")

# Rotate date labels
fig.autofmt_xdate()
fig.canvas.manager.set_window_title("My Interactive Matplotlib Window")
print(fig.dpi)

# Title
plt.title("Dual Axis Plot with Annotations and Date Formatting")

plt.tight_layout()
plt.show()
