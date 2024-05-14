using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Registry;


bool test = true;

bool doTest()
{
    return true;
}

test &= doTest();


// Console.WriteLine(test);
result = test;