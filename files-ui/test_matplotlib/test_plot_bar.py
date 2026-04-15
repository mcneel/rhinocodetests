#! python 3
import matplotlib.pyplot as plt

categories = ["A", "B", "C", "D"]
values = [10, 24, 36, 18]

plt.figure()
plt.bar(categories, values)
plt.title("Bar Plot")
plt.xlabel("Category")
plt.ylabel("Value")
plt.show()