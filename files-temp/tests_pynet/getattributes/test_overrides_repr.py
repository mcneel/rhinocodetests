BASE = object
IBASE = object

try:
    import System
    from System.Collections import IEnumerable, IEnumerator
    BASE = System.Object
    IBASE = IEnumerator
except:
    pass


# ===========================================================================
class S1(BASE):
    def __repr__(self):
        s = super().__repr__()
        return f"S1 {s}"


class S2(S1):
    def __repr__(self):
        s = super().__repr__()
        return f"S2 {s}"


s1 = S1()
assert repr(s1).startswith('S1 <__main__')

s2 = S2()
assert repr(s2).startswith('S2 S1 <__main__')

# ===========================================================================
class NS1(BASE):
    pass


class NS2(NS1):
    def __repr__(self):
        s = super().__repr__()
        return f"NS2 {s}"


ns1 = NS1()
assert repr(ns1).startswith('<__main__')

ns2 = NS2()
assert repr(ns2).startswith('NS2 <__main__')

# ===========================================================================
class IS1(IBASE):
    def __repr__(self):
        s = super().__repr__()
        return f"IS1 {s}"


class IS2(IS1):
    def __repr__(self):
        s = super().__repr__()
        return f"IS2 {s}"


is1 = IS1()
assert repr(is1).startswith('IS1 <__main__')

is2 = IS2()
assert repr(is2).startswith('IS2 IS1 <__main__')

# ===========================================================================
class INS1(IBASE):
    pass


class INS2(INS1):
    def __repr__(self):
        s = super().__repr__()
        return f"INS2 {s}"


ins1 = INS1()
assert repr(ins1).startswith('<__main__')

ins2 = INS2()
assert repr(ins2).startswith('INS2 <__main__')

# ===========================================================================
