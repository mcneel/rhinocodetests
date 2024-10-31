BASE = object
IBASE = object

try:
    import clr
    import System
    BASE = System.Object
    IBASE = System.Collections.IEnumerable
except:
    pass


class S1(BASE):
    def __getattribute__(self, name):
        print(f"S1.__getattribute__: {name}")
        return super().__getattribute__(name)

    def __getattr__(self, name):
        print(f"S1.__getattr__: {name}")
        if name == 'answer1':
            return 1
        return super().__getattr__(name)


class S2(S1):
    def __getattribute__(self, name):
        print(f"S2.__getattribute__: {name}")
        if name == 'answer2':
            return 2
        return super().__getattribute__(name)


class S3(S2):
    def __getattr__(self, name):
        print(f"S3.__getattr__: {name}")
        if name == 'answer3':
            return 3
        return super().__getattr__(name)


s1 = S1()
assert s1.answer1 == 1
try:
    assert s1.answer2 == 2
except AttributeError:
    pass

s2 = S2()
assert s2.answer1 == 1
assert s2.answer2 == 2

s3 = S3()
assert s3.answer1 == 1
assert s3.answer2 == 2
assert s3.answer3 == 3
