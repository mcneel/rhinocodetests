#! python 3
# async: false

import Rhino
from System import Action, Func, Object

def func():
    print("hi there")


def func2(arg):
    print(arg)


Rhino.RhinoApp.InvokeOnUiThread(Action(func))
Rhino.RhinoApp.InvokeOnUiThread(Func[Object, Object](func2), 12)

print("done")