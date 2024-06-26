#! python3
from rhinocode import envs

pkg= envs.pip_install_to("test_rhinocode_envs", "requests", "2.31.0")
assert str(pkg) == 'requests==2.31.0 (Any)'
assert str(pkg.Id) == 'requests'
assert str(pkg.Version) == '2.31.0'

result = True