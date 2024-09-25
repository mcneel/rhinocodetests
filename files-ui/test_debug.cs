using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;

#if DEBUG
int a =  12;
#else 
int a = 500;
#endif

int DoStuff()
{
    Console.WriteLine("Doing Stuff");
    return 12;
}

a = 50;

DoStuff();
