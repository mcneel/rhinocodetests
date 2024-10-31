import pickle


class Point3d:
    def __init__(self, p):
        self.p = p


p = Point3d(12)

# print(pickle.dumps(p))

print(p.__getstate__())
print(p.__reduce__())
