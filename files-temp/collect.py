#! python3
import sys
import gc

gc.collect()

for o in gc.get_objects():
    if 'CustomDrawable' in repr(o):
        t = repr(type(o))
        if 'wrapper_descriptor' in t \
                or 'clr._internal.CLRMetatype' in t \
                or 'function' in t \
                or 'weakref' in t:
            print(f"{sys.getrefcount(o)} {o}")
