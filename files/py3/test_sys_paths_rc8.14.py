#! python3
# https://mcneel.myjetbrains.com/youtrack/issue/RH-84188
import sys
import os
s = os.path.sep
print('\n'.join(sys.path))

path = sys.path[0]
assert f'files{s}py3' in path 

path = sys.path[1]
assert f'site-envs{s}default' in path

assert any([f'{s}scripts' in x for x in sys.path[2:4]])

result = True