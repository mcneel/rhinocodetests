#! python 3
import os
import sys

print("\nsys.paths:")
print("\n".join(sys.path))

print("\nsys.args:")
print(sys.argv, end="\n\n")

print(f"{sys.executable=}")
print(f"{sys.prefix=}")
print(f"{sys.exec_prefix=}")

print(f"file={__file__}")

# env var test
print("\nenvironment:")
for k, v in os.environ.items():
    print(f"{k}={v}")

# m = input("Hello!\n")
# print(m)

print(sys.modules.keys())

print(sys.version)



