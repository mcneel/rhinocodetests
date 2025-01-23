#! python3
import os.path as op
import urllib.request
import tempfile

url = 'https://www.rhino3d.com/inside/revit/static/images/reference/rir-interface01.png'

save_path = op.join(tempfile.gettempdir(), 'test.png')

# expect no errors here
# it might throw an SSL exception
# RH-83793
urllib.request.urlretrieve(url, save_path)

result = True