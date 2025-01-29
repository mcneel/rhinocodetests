#! python 3

import System

drives = System.IO.DriveInfo.GetDrives()
assert len(drives) > 0

result = True