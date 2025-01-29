#! python 3
import os
from System.IO import Directory

result = Directory.GetCurrentDirectory() == os.getcwd()