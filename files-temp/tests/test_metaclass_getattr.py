class CLRType:
    pass


class Meta(type):
    def __new__(cls, name, bases, dct):
        CLRType.__getattr__ = dct['__getattr__']
        return CLRType

    def __call__(cls, *args, **kwargs):
        instance = super().__call__(*args, **kwargs)
        return instance


class MyClass(metaclass=Meta):
    # pass
    def __getattr__(self, name):
        print("MyClass.__getattr__")
        if name == 'answer':
            return 42
        # return super().__getattr__(name)
        raise AttributeError



m = MyClass()
print(m.answer)
