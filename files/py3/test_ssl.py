#! python3
import os.path as op
import urllib.request
import tempfile

url = 'https://file-examples.com/wp-content/storage/2017/10/file_example_PNG_1MB.png'

save_path = op.join(tempfile.gettempdir(), 'test.png')

# expect no errors here
# it might throw an SSL exception
# RH-83793
urllib.request.urlretrieve(url, save_path)

result = True