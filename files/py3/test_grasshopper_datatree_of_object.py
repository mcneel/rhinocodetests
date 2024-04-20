#! python 3
# RH-77500 CPython fails on creating generic types with python types e.g. DataTree[object]
from Grasshopper import DataTree

dt = DataTree[object]

result = True