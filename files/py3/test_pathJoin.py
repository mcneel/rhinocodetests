from pathlib import Path

fullpath = str(Path.cwd() / "simple_example_complete.mcdx")
assert '/' in fullpath or '\\' in fullpath

result = True
