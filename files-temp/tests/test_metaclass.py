"""
- script in parsed by python
- python sees class MyClass(metaclass=Meta)
- python calls Meta.__new__ to create the type for MyClass
  this effectively turns MyClass => <class '__main__.CLRType'>
- python runs the rest of script, instantiating MySubclass()
- since MyClass<CLRType> is the base of MySubclass, its __new__ method is called
- m is ready to use
- m.DoWork() calls the MyClass<CLRType>.DoWork() method
- m() calls MyClass<CLRType> __call__ since this is handled on instance


NOTE:
if Meta.__new__ is not defined, MyClass remains unchanged
as MyClass => <class '__main__.MyClass'>
MyClass.__call__ is handled by Meta.__call__
then on instantiating MySubclass(), Meta.__call__ will be called
"""

class CLRType:
    def __new__(cls):
        print("CLRType.__new__")
        return super().__new__(cls)

    def __call__(self, *args, **kwargs):
        print("CLRType.__call__")


class Meta(type):
    def __new__(cls, name, bases, dct):
        print("Meta.__new__")
        # dct: {'__module__': '__main__', '__qualname__': 'MyClass', 'DoWork': <function MyClass.DoWork at 0x10f2ea710>}
        print(f"\tcls   = {cls}")
        print(f"\tname  = {name}")
        print(f"\tbases = {bases}")
        print(f"\tdct   = {dct}")
        CLRType.DoWork = dct['DoWork']
        return CLRType

    def __call__(cls, *args, **kwargs):
        print("Meta.__call__")
        instance = super().__call__(*args, **kwargs)
        return instance


class MyClass(metaclass=Meta):
    def DoWork(self):
        print("MyClass.DoWork()")


class MySubclass(MyClass):
    def __init__(self) -> None:
        super().__init__()


print(f"MyClass => {MyClass}")

m = MySubclass()
print(f"\tcls   = {m.__class__}")
print(f"\tbases = {m.__class__.__bases__}")
print(f"\tinst? = {isinstance(m, MyClass)}")
m.DoWork()

MySubclass()
MySubclass()
MySubclass()
MySubclass()

m()
