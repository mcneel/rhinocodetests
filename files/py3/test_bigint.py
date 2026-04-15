#! python 3
# from clr._internal import CLRMetatype
import System as S
from System.Numerics import BigInteger
import math

s = "string"
f = math.factorial(255)
k = S.Int32.MaxValue
b = BigInteger.Parse(str(f))
result = type(b).__class__.__module__ == 'clr._internal' \
     and type(b).__class__.__name__ == 'CLRMetatype'