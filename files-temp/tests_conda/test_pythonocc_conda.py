#! python 3
import conda_interop

conda_interop.append_env(r'C:\Users\ein\.conda\envs\wood-dev')

# now can import modules from conda environment
import OCC
print(OCC)
