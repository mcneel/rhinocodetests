#! python3
import rhinocode

print(rhinocode.RHINOCODE_DIR)

from rhinocode import python_server
assert python_server.PROCESS_ID == 0
assert python_server.PROCESS_DEBUG == False

from rhinocode import python

print(python.RUNTIME_DIR)
print(python.SCRIPTS_DIR)
print(python.CACHES_DIR)
print(python.SITE_ENVS_DIR)
print(python.SITE_RHINOPYTHON_DIR)
print(python.SITE_STUBS_DIR)
print(python.SITE_SUPPORT_DIR)