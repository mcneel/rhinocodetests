#! python3
import os
import rhinocode

p = rhinocode.get_python_executable() # RH-83790
result = os.path.exists(p)
