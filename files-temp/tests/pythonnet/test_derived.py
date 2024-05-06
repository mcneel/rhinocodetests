import clr

clr.AddReference(
    r"/Users/ein/gits/McNeel/rhinocodeplugins/src/support/PyNetTests/bin/Debug/pynettests.dll"
)
from PyNetTests import *


import System
from System.Collections import Hashtable

# # ==============================================================================
# print("Pure Python" + "="*10)
# class Base:
#     def __init__(self):
#         print("Base")


# class Derived(Base):
#     def __init__(self):
#         super().__init__()
#         print("Derived")


# m = Derived()

# # ==============================================================================

# class MyTable(Hashtable):
#     # class MyTable(IndexerClass):
#     def how_many(self):
#         return self.Count

#     # def __getitem__(self, key):
#     #     print("MyTable.__getitem__")
#     #     value = 12
#     #     value = super().__getitem__(key)
#     #     return "my " + str(value)

#     def __setitem__(self, key, value):
#         print("MyTable.__setitem__")
#         return super().__setitem__(key, value + "-set")


# table = MyTable()
# table[1] = "one"
# print(table[1])
# table["two"] = "two"
# table["three"] = "three"

# print(type(table.__getitem__))
# print(MyTable.__bases__)
# print(MyTable.__mro__)
# assert table["one"] == "my one"
# assert table["two"] == "my two"
# assert table["three"] == "my three"

# assert table.Count == 3

# IndexerClassConsumer.Take(table, 1);


# ==============================================================================
# print("Python Implementing dotnet" + "=" * 10)
# class Sub(System.Exception):
#     pass

# e = Sub()
# print(e)
# print(isinstance(e, Exception))

# ==============================================================================
# print("Python Implementing dotnet" + "=" * 10)
# class TestInterface(IBase):
#     def Foo(self):
#         return "InterfaceTestClass"

#     def Bar(self, i):
#         return f"Bar: {i}"


# e = TestInterface()
# print(e)
# print(e.Foo())
# print(e.Bar(12))

# ==============================================================================
# print("Python Implementing dotnet" + "=" * 10)

# class OtherPublic(PublicBaseClass):
#     __namespace__ = "Python.Test.BZZZ"

#     def __init__(self, v):
#         # super().__init__(v)
#         PublicBaseClass.__init__(self, v)
#         print("Init'd")
#         self.value2 = v

#     def DoWorkWith(self, value):
#         r = super().DoWorkWith(value)
#         print(f"did work with value: {r}")
#         return r + 10

#     def DoOtherWork(self, value):
#         r = super().DoOtherWork(value)
#         print(f"did other work with value: {r}")
#         return r + 10

#     def Get(self, key):
#         value = super().__getitem__(key)
#         return "my " + str(value)

#     def do_other_work(self):
#         r = PublicBaseClass.DoOtherWork(self)
#         return r


# op = OtherPublic(42)
# assert op.Value == 42
# assert op.value2 == 42
# print(op.Value)
# op.DoWork()
# op.DoWorkWith(12)
# op.Get(12)
# print(op.do_other_work())

# PublicClassConsumer.TakeBaseClassVirtual(op, 50)


# class SubClass(BaseClassA):
#     def __init__(self, v):
#         super().__init__(v)
#         self.value2 = 12


# class SubClass(BaseClassB):
#     def __init__(self, v):
#         super().__init__(v)
#         self.value2 = 12


# inst = SubClass("test")
# print(inst.value, inst.value2)


class OverloadingSubclass(GenericVirtualMethodTest):
    pass


class OverloadingSubclass2(OverloadingSubclass):
    pass


obj = OverloadingSubclass()
print(obj)
print(obj.VirtMethod[int](5))
assert obj.VirtMethod[int](5) == 5

obj = OverloadingSubclass2()
print(obj)
print(obj.VirtMethod[int](5))
assert obj.VirtMethod[int](5) == 5


# ==============================================================================
# print("Python re-implementing dotnet subclass" + "=" * 10)

# class OtherOtherPublic(OtherPublic):
#     def __init__(self):
#         super().__init__(42)
#         print("Init'd Subclassed")

    # def DoWorkWith(self, value):
    #     r = super().DoWorkWith(value) + 10
    #     print(f"Other-Other did work with value: {r}")


# oop = OtherOtherPublic()
# print(oop)
# oop.DoWorkWith(12)

# PublicClassConsumer.TakeBaseClassVirtual(oop, 50)
# # ==============================================================================
# print("Python implementing dotnet abstract" + "="*10)
# class Implementation(AbstractBaseClass):
#     def __init__(self):
#         super().__init__()
#         print("Init'd")

#     def get_ImplThisPublicProperty(self):
#         return 42

#     def __getitem__(self):
#         print(f"did work")

#     def DoWork(self):
#         print(f"did work")


# m = Implementation()
# m.DoWork()
# # m.ImplThisPublicProperty = 12
# # AbstractClassConsumer.TakeAbstractClass(m)
# AbstractClassConsumer.TakeAbstractClassProperty(m)
# AbstractClassConsumer.TakeAbstractClassVirtual(m, 50)


# print("Python implementing dotnet abstract" + "="*10)
# class AnotherImplementation(AbstractBaseClass):
#     # using a different base constructor
#     def __init__(self, value):
#         super().__init__(value)
#         print(f"Init'd: {value}")

#     def ImplThisPublicMethod(self):
#         print('ImplThisPublicMethod')

#     def DoWorkWith(self, value):
#         # virtual methods are overriden to call the python implementation.
#         # calling base virtual method will result in infinite loop
#         r = super().DoWorkWith(value)
#         print(f"did work with value: {value}")


# m = AnotherImplementation(12)
# m.DoWork()
# m.DoWorkWith(12)
# m.ImplThisPublicMethod()
