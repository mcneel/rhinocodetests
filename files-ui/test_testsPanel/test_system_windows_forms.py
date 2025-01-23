#! python 3
import clr
clr.AddReference("System.Windows.Forms")
import System

# deriving class should not fail
# https://mcneel.myjetbrains.com/youtrack/issue/RH-81356
class TestForm(System.Windows.Forms.Form):
    def __init__(self):
        super().__init__()


form = TestForm()
result = form is not None