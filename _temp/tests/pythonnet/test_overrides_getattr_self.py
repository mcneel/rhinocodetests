from typing import Any
import System


class T1(System.Object):
    def __init__(self, value) -> None:
        self.answer = value

    def __getattr__(self, key):
        if key == 'answer':
            return self.answer
        return super().__getattr__()


class T2(System.Object):
    def __init__(self, value) -> None:
        self.answer = value

    def __getattribute__(self, __name: str) -> Any:
        if __name == 'answer':
            return 43
        return super().__getattribute__(__name)

    def __getattr__(self, key):
        if key == 'answer':
            return self.answer
        return super().__getattr__()


class T3(System.Object):
    answer = 44

    def __getattr__(self, key):
        if key == 'answer':
            return self.answer
        return super().__getattr__()


class T4(System.Object):
    def __getattribute__(self, __name: str) -> Any:
        if __name == 'answer':
            return 45
        return super().__getattribute__(__name)

    def __getattr__(self, key):
        if key == 'answer':
            return self.answer
        return super().__getattr__()


t1 = T1(42)
assert t1.answer == 42

t2 = T2(42)
assert t2.answer == 43

t3 = T3()
assert t3.answer == 44

t4 = T4()
assert t4.answer == 45