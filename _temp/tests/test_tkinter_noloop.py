from tkinter import *

ROOT = Tk()
LABEL = Label(ROOT, text="Hello, world!")
LABEL.pack()
LOOP_ACTIVE = True
while LOOP_ACTIVE:
    ROOT.update()
    USER_INPUT = input("Give me your command! Just type \"exit\" to close: ")
    if USER_INPUT == "exit":
        ROOT.quit()
        LOOP_ACTIVE = False
    else:
        LABEL = Label(ROOT, text=USER_INPUT)
        LABEL.pack()