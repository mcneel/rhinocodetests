#! python 3
# async: true

import threading

import Rhino
from System import Action


def func():
    Rhino.RhinoApp.WriteLine("hi there")


class Worker(threading.Thread):
    def __init__(self, thread_id, name):
        threading.Thread.__init__(self)
        self.thread_id = thread_id
        self.name = name

    def run(self):
        Rhino.RhinoApp.WriteLine(self.name)
        Rhino.RhinoApp.InvokeOnUiThread(Action(func))


threads = []
thread1 = Worker(1, 'Worker-1')
thread1.start()
threads.append(thread1)

for t in threads:
    t.join()