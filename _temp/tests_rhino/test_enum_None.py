#! python 3
# https://mcneel.myjetbrains.com/youtrack/issue/RH-80145
import Rhino

v = Rhino.DocObjects.ObjectType.Curve
if not (v & Rhino.DocObjects.ObjectType.Light):
    print("Pass: v is not Light")

v = Rhino.DocObjects.ObjectType.Light
if v & Rhino.DocObjects.ObjectType.Light:
    print("Pass: v is Light")
