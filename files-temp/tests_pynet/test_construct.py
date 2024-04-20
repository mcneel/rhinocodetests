import testing as p


def test_construct_from_abstract_with_public_ctor():
    p.assert_equal(
        target=p.Script("tests_pynet/scripts/abstract_public_ctor.py"),
        result="PyNetTests.AbstractBaseClass\nDoing Work\nOK",
    )
