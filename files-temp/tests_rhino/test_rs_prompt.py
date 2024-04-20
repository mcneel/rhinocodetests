#! python 2

import rhinoscriptsyntax as rs


def DoSomething():
    rs.Prompt("Processing, please wait")
    rs.Sleep(2000)


DoSomething()