import sys
import platform
import os.path as op
from typing import List, Optional, Dict
import subprocess


ROOT_PATH = op.dirname(op.dirname(__file__))

MACAPP_PLUGINS_DIR = (
    r"src/support/RhinoCode/Debug/RhinoCode.app/Contents/MonoBundle/Plugins"
)
MACAPP_RESOURCE_DIR = r"src/support/RhinoCode/Debug/RhinoCode.app/Contents/Resources"

WINAPP_DIR = r"src\support\RhinoCode\Debug"
WINAPP_PLUGINS_DIR = r"src\support\RhinoCode\Debug"


class Target:
    def __init__(self, option, value) -> None:
        self.option = option
        self.value = value


class Command(Target):
    def __init__(self, command) -> None:
        super().__init__("-c", command)


class Script(Target):
    def __init__(self, script) -> None:
        script_path = op.normpath(op.join(ROOT_PATH, script))
        super().__init__("-s", script_path)


def _clean_stream(stream: bytes):
    return "\n".join(stream.decode().strip().splitlines())


def _system(
    args: List[str],
    cwd: Optional[str] = None,
    dump_stdout: Optional[bool] = False,
) -> str:
    """Run a command and return the stdout"""
    if dump_stdout:
        res = subprocess.run(args, stderr=subprocess.STDOUT, check=False, cwd=cwd)
        return "", ""
    else:
        res = subprocess.run(args, capture_output=True, check=False, cwd=cwd)
        return _clean_stream(res.stdout), _clean_stream(res.stderr)


def _run_pynettest(target: Target):
    if sys.platform == "darwin":
        dotnet_bin = op.join(
            ROOT_PATH, MACAPP_RESOURCE_DIR, rf"dotnet/{platform.machine()}/dotnet"
        )
        pynettest_dll = op.join(ROOT_PATH, MACAPP_PLUGINS_DIR, "pynettests.dll")
        return _system([dotnet_bin, pynettest_dll, target.option, target.value])
    elif sys.platform == "win32":
        pynettest_bin = op.join(ROOT_PATH, WINAPP_PLUGINS_DIR, "pynettests.exe")
        return _system([pynettest_bin, target.option, target.value])


def assert_equal(target: Target, result: str):
    out, err = _run_pynettest(target)
    print(repr(out))
    print(err)
    assert out == result


def assert_contains(target: Target, result: str):
    out, err = _run_pynettest(target)
    print(err)
    assert result in out
