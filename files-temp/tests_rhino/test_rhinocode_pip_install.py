#! python3

# decide which requests to install
from rhinocode import envs
OLD = False

if OLD:
    envs.pip_install("requests", "2.17.2")
else:
    envs.pip_install("requests", "2.31.0")

# load requests
import requests
print(requests)