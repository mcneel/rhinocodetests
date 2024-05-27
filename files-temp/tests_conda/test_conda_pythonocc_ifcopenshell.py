#! python 3
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
    
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'TKernel.dll'))
    
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'TKMath.dll'))
    
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'zlib.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'jpeg8.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'tiff.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'libpng16.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'openjp2.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'libwebpmux.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'libwebp.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'raw.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'Iex.dll'))
    # ctypes.WinDLL(r'C:\Users\ein\.rhinocode\py39-rh8\vcruntime140.dll')
    # ctypes.WinDLL(r'C:\Users\ein\.rhinocode\py39-rh8\vcruntime140_1.dll')

    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'msvcp140.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'vcruntime140.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'vcruntime140_1.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'api-ms-win-crt-runtime-l1-1-0.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'api-ms-win-crt-heap-l1-1-0.dll'))
    ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'IlmThread.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'OpenEXR.dll'))
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'FreeImage.dll'))
    
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'TKService.dll'))
    
    # ctypes.WinDLL(op.join(conda_env_path, r'Library\bin', 'TKV3d.dll'))


connect_conda(r'C:\Users\ein\.conda\envs\wood-dev')

from OCC.Core import _Units
from OCC.Core import _V3d

# import OCC
# import OCC.Core
# import OCC.Core.TopoDS

# import ifcopenshell
# import ifcopenshell.geom as geom

# print(ifcopenshell)
# print(geom)