#! python 2

import Eto
import Rhino
import scriptcontext

class MyTestDialog(Eto.Forms.Dialog[bool]):
    def __init__(self):
        
        self.Counter = 1
        
        self.Title = "Test Print problem"
        self.Size = Eto.Drawing.Size(300,150)
        self.Padding = Eto.Drawing.Padding(10)
        self.TopMost = True
        
        self.label = Eto.Forms.Label(Text="See something in command line yet ?")
        self.label.TextAlignment = Eto.Forms.TextAlignment.Center
        
        self.button_print = Eto.Forms.Button(Text="Click me to print text")
        self.button_print.Click += self.OnPrintButtonClick
        
        self.button_OK = Eto.Forms.Button(Text="OK")
        self.button_OK.Click += self.OnOkButtonClick
        
        self.layout = Eto.Forms.DynamicLayout()
        self.layout.DefaultSpacing = Eto.Drawing.Size(0, 10)
        self.layout.AddRow(self.label)
        self.layout.AddRow(self.button_print)
        self.layout.AddSpace()
        self.layout.AddRow(self.button_OK)
        self.Content = self.layout
        
        self.DefaultButton = self.button_OK
        self.LoadComplete += self.OnFormLoadComplete
    
    def OnFormLoadComplete(self, sender, e):
        print "OnFormLoadComplete Event printed successfully!"
    
    def OnPrintButtonClick(self, sender, e):
        print "You have clicked the button {} times".format(self.Counter)
        self.Counter += 1
    
    def OnOkButtonClick(self, sender, e):
        self.Close(True)
    
def DoSomething():
    dialog = MyTestDialog()
    Rhino.UI.EtoExtensions.ShowSemiModal(
                                        dialog,
                                        scriptcontext.doc.ActiveDoc, 
                                        Rhino.UI.RhinoEtoApp.MainWindow
                                        )
DoSomething()