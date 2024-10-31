import testing as p


def test_python_version():
    p.assert_contains(
        target=p.Command("import sys;print(sys.version)"),
        result="3.9.10",
    )


def test_refcount_stdout():
    p.assert_contains(
        target=p.Command("import sys;print(sys.getrefcount(sys.stdout))"),
        result="3",
    )
