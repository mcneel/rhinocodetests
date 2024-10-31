import dll_loader
from PyNetTests import AbstractBaseClass, AbstractBaseClassConsumer  # type: ignore


class MyClass(AbstractBaseClass):
    def __init__(self) -> None:
        super().__init__()

    def DoWork(self):
        print(12)


m = MyClass()
AbstractBaseClassConsumer.TakeBaseClass(m)

print("OK")
