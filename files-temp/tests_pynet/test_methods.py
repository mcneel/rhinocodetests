import testing as p


def test_methods_with_out_params():
    p.assert_equal(
        target=p.Script("tests_pynet/scripts/outparam.py"),
        result="(True, 42)\n(True, 42.42)",
    )
