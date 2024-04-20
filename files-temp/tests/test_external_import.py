#! python 2
import sys
import os.path as op

p = op.join(op.dirname(op.dirname(__file__)), r"tests_projects/libraries/")
if p not in sys.path:
    sys.path.append(p)

print('\n'.join(sys.path))

import testipymodule

print(testipymodule)