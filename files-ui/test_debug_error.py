#! python3

import rhinoscriptsyntax as rs

# rs.AddAlias()


class TestDebug:
    def __init__(self):
        pass

    
    def Test_Debug_Error(self):
        raise Exception("test debug error")


m = TestDebug();
# -------------------
# put a breakpoint here and debug
# step into the method call
# step over the exception and it should throw error
# -------------------
m.Test_Debug_Error()
