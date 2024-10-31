#! python3

import sys
import _distutils_hack

# sys.meta_path.insert(0, _distutils_hack.DistutilsMetaFinder)
for m in sys.meta_path:
    print(m)