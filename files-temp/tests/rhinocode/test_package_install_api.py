#! python3
"""Test installing a package programmatically

This is an example python script that decides which version of `requests` to install.
"""

# decide which requests to install
OLD = False

import clr
clr.AddReference("Rhino.Runtime.Code")
import System
from Rhino.Runtime.Code import RhinoCode as RC
from Rhino.Runtime.Code.Languages import ILanguage, LanguageSpec
from Rhino.Runtime.Code.Environments import PackageSpec

python3 = RC.Languages.WherePasses(LanguageSpec("*.*.python", "3")) \
                      .QueryLatest()

print(python3.Environs.Shared)


old_requests = PackageSpec("requests", System.String("2.17.2"))
new_requests = PackageSpec("requests", System.String("2.31.0"))
latest_requests = PackageSpec("requests")

p = python3.Environs.Shared.AddPackage(
        old_requests if OLD else new_requests
    )
print(p)


# load requests
import requests
print(requests)