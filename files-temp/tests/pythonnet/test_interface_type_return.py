import Eto.Forms as forms

m_listbox = forms.ListBox()

item = forms.ListItem()
item.Text = "Test"
item.Tag = "Tag"
m_listbox.Items.Add(item)

for item in m_listbox.Items:
    print(type(item))
    print(dir(item))
    print(item.Tag)