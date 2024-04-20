#! python 3
# https://github.com/mcneel/rhino/pull/67944
# https://discourse.mcneel.com/t/rhinocode-scripeditor-for-development-of-libraries/175228/22
# https://github.com/petrasvestartas/compas_wood.git

import os
import os.path as op
import sys
import ctypes

CONDA_ENV = r'C:\Users\ein\.conda\envs\wood-dev'
COMPAS_WOOD_PATH = r'C:\Users\ein\compas_wood\src'

# add the paths of site-packages in conda environment so other packages that compas_wood
# depend on can be found e.g. compas
sys.path.append(op.join(CONDA_ENV, r"Lib\site-packages"))

# add the location of compas_wood source so we can import this
sys.path.append(COMPAS_WOOD_PATH)

# tell python where it can find dlls. this is required to find all other .dll files that
# are installed as part of the other packages in the conda environment e.g. fblas
os.add_dll_directory(op.join(CONDA_ENV, r'Library\bin'))

# tell python where the wood_pybind11*.pyd is located
os.add_dll_directory(COMPAS_WOOD_PATH)

# this is a good test to see if dll load works
# ctypes.WinDLL(op.join(CONDA_ENV, r'Library\bin\mpir.dll'))

# now we can import the module and test
from compas_wood.joinery import test

print(test)
test()
