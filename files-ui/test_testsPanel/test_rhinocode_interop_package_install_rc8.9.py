#! python3
from rhinocode import envs

pkg= envs.pip_install("urllib3", "2.0.7")
assert str(pkg) == 'urllib3==2.0.7 (Any)'
assert str(pkg.Id) == 'urllib3'
assert str(pkg.Version) == '2.0.7'


result = True