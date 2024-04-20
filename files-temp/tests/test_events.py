import clr
clr.AddReference(r"/Users/ein/gits/McNeel/rhinocodeplugins/src/support/PyNetTests/bin/Debug/pynettests.dll")
from PyNetTests import EventTestClass, EventHandlerTest, EventArgsTest



def Handler(sender, args):
    print(sender, args.value)


class GenericHandler(object):
    def __init__(self):
        self.value = None

    def handler(self, sender, args):
        self.value = args.value


ob = EventTestClass()
handler = GenericHandler()

#d = Handler
#d= handler.handler
#d = EventHandlerTest(handler.handler)

ob.add_PublicEvent(d)
ob.OnPublicEvent(EventArgsTest(10))
ob.remove_PublicEvent(d)
ob.OnPublicEvent(EventArgsTest(20))
print(handler.value)