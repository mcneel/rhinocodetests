#! python3

# https://mcneel.myjetbrains.com/youtrack/issue/RH-79880

import os

working_dir = os.path.dirname(os.path.realpath(__file__))
module_path = os.path.join(working_dir, "testcounter.py")
# Check if we've created the test module we're going to import later 
if os.path.isfile(module_path):
    import testcounter
    print("Found module, counter is", testcounter.counter)
    with open(module_path, "w") as f:
        f.write("counter = " + str(testcounter.counter + 1) + "\n")
else:
    print("Creating test module at count = 0")
    with open(module_path, "w") as f:
        f.write("counter = 0\n")

