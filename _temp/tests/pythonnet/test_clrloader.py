# r: clr-loader
# https://pythonnet.github.io/clr-loader/usage.html#getting-a-callable-function
import clr_loader as p

runtime = p.get_coreclr()

print(runtime.info())
