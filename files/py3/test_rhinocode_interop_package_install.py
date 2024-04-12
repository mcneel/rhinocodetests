#! python3
from rhinocode import envs

pkg= envs.pip_install("requests", "2.31.0")
assert str(pkg) == 'requests==2.31.0 (Any)'
assert str(pkg.Id) == 'requests'
assert str(pkg.Version) == '2.31.0'

import requests
assert requests is not None

result = True