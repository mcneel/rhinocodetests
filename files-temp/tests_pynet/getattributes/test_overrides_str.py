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
    def __str__(self):
        s = super().__str__()
        return f"S1 {s}"


class S2(S1):
    def __str__(self):
        s = super().__str__()
        return f"S2 {s}"


s1 = S1()
assert str(s1).startswith('S1 __main__')

s2 = S2()
assert str(s2).startswith('S2 S1 __main__')

# ===========================================================================
class NS1(BASE):
    pass


class NS2(NS1):
    def __str__(self):
        s = super().__str__()
        return f"NS2 {s}"


ns1 = NS1()
assert str(ns1).startswith('__main__')

ns2 = NS2()
assert str(ns2).startswith('NS2 __main__')

# ===========================================================================
class IS1(IBASE):
    def __str__(self):
        s = super().__str__()
        return f"IS1 {s}"


class IS2(IS1):
    def __str__(self):
        s = super().__str__()
        return f"IS2 {s}"


is1 = IS1()
assert str(is1).startswith('IS1 __main__')

is2 = IS2()
assert str(is2).startswith('IS2 IS1 __main__')

# ===========================================================================
class INS1(IBASE):
    pass


class INS2(INS1):
    def __str__(self):
        s = super().__str__()
        return f"INS2 {s}"


ins1 = INS1()
assert str(ins1).startswith('__main__')

ins2 = INS2()
assert str(ins2).startswith('INS2 __main__')

# ===========================================================================
