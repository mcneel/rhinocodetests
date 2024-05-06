#! python 3
# requirements: requests

import requests

def list_package_versions_files(package_name):
    # url = f"https://pypi.org/pypi/{package_name}/json"
    url = f"https://pypi.org/simple/{package_name}/json"
    response = requests.get(url)
    data = response.json()

    for version, releases in data["releases"].items():
        print(f"Version: {version}")
        for release in releases:
            fname = release['filename']
            if 'win_amd64' in fname:                
                print(f"- Filename: {release['filename']}")
                # print(f"  URL: {release['url']}")
                # print(f"  Python version: {release['python_version']}")
                # print(f"  Packagetype: {release['packagetype']}")
        print("")

package_name = "ifcopenshell"
list_package_versions_files(package_name)
