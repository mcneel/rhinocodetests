import sys
from System import Uri


class Test:
    def __init__(self):
        self.some_value = 12

    def SomeMethod(self):
        print(self.some_value)


def func_call_test(a, b):
    def nested_func_call_test(c):
        d = Test()
        u = Uri("http://test.com")
        print(d, u, c)

    print(a, b)
    nested_func_call_test(a + b)


print(1)

print('\n'.join(sys.path))

print(sys.gettrace())

func_call_test(10, 10)

for i in range(3):
    print(i)

g = Test()
some_list = [1, 2, 3, 4]
print(2)
some_list[2] = 42
print("last")
