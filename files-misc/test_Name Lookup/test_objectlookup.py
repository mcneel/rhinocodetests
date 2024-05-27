import time
import rhinoscriptsyntax as rs

start = time.time()

m = None
for i in range(1000000):
    m = rs.ObjectsByName("Jack")

end = time.time()
print(end - start)
print(m)
