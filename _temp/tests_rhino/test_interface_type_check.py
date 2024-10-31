import Eto.Forms as forms

m_listbox = forms.ListBox()

item = forms.ListItem()
item.Text = "Test"
item.Tag = "Tag"
m_listbox.Items.Add(item)

for item in m_listbox.Items:
    # print(type(item))
    # print(type(item.__implementation__))
    # print(type(item.__raw_implementation__))
    assert item.Tag == "Tag"
    assert isinstance(item, forms.ListItem)
    assert isinstance(item, forms.IListItem)

print('PASS')