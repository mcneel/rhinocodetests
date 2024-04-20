#! python 3
import Rhino.UI
import Eto.Drawing as drawing
import Eto.Forms as forms

class TestButtonDialog(forms.Dialog[bool]):
    
    def __init__(self):
        # NOTE: Call super init
        super().__init__()
        Rhino.UI.EtoExtensions.UseRhinoStyle(self)
        self.Title = "Brewing Styles"
        self.Padding = drawing.Padding(5)
        self.Resizable = False
        
        layout = forms.DynamicLayout()
        layout.Padding = drawing.Padding(5)
        layout.Spacing = drawing.Size(5, 5)        
        
        label = forms.Label()
        label.Text = 'Pick your coffee brewing style:'
        layout.AddRow(label)
        layout.AddRow(None) # spacer
        
        self.Labels = []
        self.Labels.append('Drip Brew')
        self.Labels.append('Pour Over')
        self.Labels.append('Cold Brew')
        self.Labels.append('Espresso')
        self.Labels.append('Ristretto')
        
        for text in self.Labels:
            button = forms.Button()
            button.Text = text
            button.Tag = text
            button.Click += self.OnButtonClick
            layout.AddRow(button)
        layout.AddRow(None) # spacer
        
        layout.AddRow(self.CreateOKButton())
        layout.AddRow(None) # spacer
        self.Content = layout
    
    def CreateOKButton(self):
        # NOTE: can not specify .Text in ctor like ironpython
        self.DefaultButton = forms.Button()
        self.DefaultButton.Text = 'OK'
        self.DefaultButton.Click += self.OnOkButtonClick
        layout = forms.DynamicLayout()
        layout.Spacing = drawing.Size(5, 5)
        layout.AddRow(None, self.DefaultButton, None)
        return layout
        
    def OnButtonClick(self, sender, e):
        if isinstance(sender, forms.Button):
            print(sender.Tag)
            sender.Text = sender.Tag + " Picked"

    def OnOkButtonClick(self, sender, e):
        self.Close(True)
    
def test_eto_button_dialog():
    dialog = TestButtonDialog()
    rc = dialog.ShowModal(Rhino.UI.RhinoEtoApp.MainWindow)
    print(rc)

if __name__ == "__main__":
    test_eto_button_dialog()    
    