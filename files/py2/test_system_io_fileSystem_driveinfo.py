#! python 2

import System

import clr
clr.AddReference("System.IO.FileSystem.DriveInfo")

drives = System.IO.DriveInfo.GetDrives()
assert len(drives) > 0

result = True