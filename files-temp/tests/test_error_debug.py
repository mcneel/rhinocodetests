#! python 3

import sys
from System import Uri


def func_call_test(a, b):
    def nested_func_call_test(c):
        raise Exception("I don't like you")

    nested_func_call_test(a + b)


func_call_test(10, 10)


# some other code

func_call_test(5, 5)