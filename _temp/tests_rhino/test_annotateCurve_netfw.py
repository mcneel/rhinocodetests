#! python 3

# with a textbox that can be used to define the text in a new text dot
from System.Windows.Forms import Form, DialogResult, Label, Button, TextBox
from System.Drawing import Point, Size

import rhinoscript.selection
import rhinoscript.geometry

# Our custom form class
class AnnotateForm(Form):
    # build all of the controls in the constructor
    def __init__(self, curveId):
        super().__init__()

        offset = 10
        self.Text = "Annotate Curve"
        crvlabel = Label()
        crvlabel.Text = "Curve ID = " + str(curveId)
        crvlabel.AutoSize = True

        self.Controls.Add(crvlabel)
        width = crvlabel.Right
        pt = Point(crvlabel.Left, crvlabel.Bottom + offset)
        labelstart = Label()
        labelstart.Text = "Text at start"
        labelstart.AutoSize = True
        labelstart.Location = pt
        self.Controls.Add(labelstart)
        pt.X = labelstart.Right + offset
        inputstart = TextBox()
        inputstart.Text = "Start"
        inputstart.Location = pt
        self.Controls.Add(inputstart)
        if inputstart.Right > width:
            width = inputstart.Right
        self.m_inputstart = inputstart

        pt.X = labelstart.Left
        pt.Y = labelstart.Bottom + offset * 3
        buttonApply = Button()
        buttonApply.Text = "Apply"
        buttonApply.DialogResult = DialogResult.OK
        buttonApply.Location = pt
        self.Controls.Add(buttonApply)
        pt.X = buttonApply.Right + offset
        buttonCancel = Button()
        buttonCancel.Text = "Cancel"
        buttonCancel.DialogResult = DialogResult.Cancel
        buttonCancel.Location = pt
        self.Controls.Add(buttonCancel)
        if buttonCancel.Right > width:
            width = buttonCancel.Right
        self.ClientSize = Size(width, buttonCancel.Bottom)
        self.AcceptButton = buttonApply
        self.CancelButton = buttonCancel

    def TextAtStart(self):
        return self.m_inputstart.Text


# prompt the user to select a curve
curveId = rhinoscript.selection.GetObject(
    "Select a curve", rhinoscript.selection.filter.curve
)
if curveId is None:
    print("no curve selected")
else:
    location = rhinoscript.curve.CurveStartPoint(curveId)
    if location is not None:
        form = AnnotateForm(curveId)
        if form.ShowDialog() == DialogResult.OK:
            # this block of script is run if the user pressed the apply button
            text = form.TextAtStart()
            if len(text) > 0:
                # create a new text dot at the start of the curve
                rhinoscript.geometry.AddTextDot(text, location)
