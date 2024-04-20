#! python 3

import Rhino

for p in Rhino.ApplicationSettings.FileSettings.GetSearchPaths():
    print(p)