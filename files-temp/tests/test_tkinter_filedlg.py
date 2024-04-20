#! python3
import os
import tkinter as tk
from tkinter.filedialog import askdirectory, askopenfilename

root = tk.Tk()
root.withdraw()
PPath = askdirectory(
    title="Please select your installation folder location",
    initialdir=r"C:\Program Files\\",
)

t = "Please select jdk file"
if os.path.exists(os.path.expanduser("~\Documents")):
    FFile = askopenfilename(
        filetypes=(("jdk file", "*.jdk"), ("All Files", "*.*")),
        title=t,
        initialdir=os.path.expanduser("~\Documents"),
    )
else:
    FFile = askopenfilename(
        filetypes=(("jdk file", "*.jdk"), ("All Files", "*.*")), title=t
    )