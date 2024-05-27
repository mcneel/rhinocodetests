#! python 3
# requirements: OpenEXR
import os
import os.path as op
import sys
import ctypes

def connect_conda(conda_env_path: str):
    """Wire up search paths to conda environment
    
    Args:
        conda_env_path (str): absolute root path of conda environment
    """
    # add the paths of site-packages in conda environment so packages can be found e.g. compas
    sys.path.append(op.join(conda_env_path, r"Lib\site-packages"))

    # tell python where it can find dlls. this is required to find all other .dll files that
    # are installed as part of the other packages in the conda environment e.g. fblas
    os.add_dll_directory(op.join(conda_env_path, r'bin'))
    os.add_dll_directory(op.join(conda_env_path, r'Library\bin'))
    os.add_dll_directory(op.join(conda_env_path, r'Library\lib'))


connect_conda(r'C:\Users\ein\.conda\envs\wood-dev')


import OpenEXR

print(OpenEXR)