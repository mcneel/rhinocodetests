import os.path as op

from Rhino.PlugIns import PlugIn


plugin = op.dirname(PlugIn.PathFromName("SomeGreatPlugin"))
data = op.join(plugin, "shared/data.txt")
print(data)
