#! python3
import clr
clr.AddReference("Rhino.Runtime.Code")

import System
from Rhino.Runtime.Code import RhinoCode as RC
from Rhino.Runtime.Code.Languages import ILanguage, LanguageSpec
from Rhino.Runtime.Code.Environments import PackageSpec

python3 = RC.Languages.WherePasses(LanguageSpec("*.*.python", "3")) \
                      .QueryLatest()

assert python3.Environs.Shared is not None

pkg = python3.Environs.Shared.AddPackage(PackageSpec("requests", System.String("2.31.0")))
assert str(pkg) == 'requests==2.31.0 (Any)'
assert str(pkg.Id) == 'requests'
assert str(pkg.Version) == '2.31.0'


import requests
assert requests is not None

result = True