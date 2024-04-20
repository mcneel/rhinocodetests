import Rhino


class DrawCurvesConduit(Rhino.Display.DisplayConduit):
    def __init__(self):
        super().__init__()


m = DrawCurvesConduit()

print(m)
