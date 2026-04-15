#! python 3
import matplotlib.pyplot as plt

labels = ["Python", "C++", "Ruby", "Java"]
sizes = [215, 130, 245, 210]

plt.figure()
plt.pie(sizes, labels=labels, autopct="%1.1f%%", startangle=140)
plt.title("Pie Chart")
plt.axis("equal")
plt.show()