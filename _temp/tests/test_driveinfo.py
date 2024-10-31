#! python 3

import System

drives = System.IO.DriveInfo.GetDrives()
for drive in drives:
    print(drive)