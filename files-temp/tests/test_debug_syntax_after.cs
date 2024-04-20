using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Rhino;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 14, 1, System.Array.Empty<(string,object)>());
int a = 500;

int DoStuff()
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 17, 0, new (string, object)[] {("a", a)});
try{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 19, 1, new (string, object)[] {("a", a)});
    Console.WriteLine("Doing Stuff");
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 20, 1, new (string, object)[] {("a", a)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 20, 2, new (string, object)[] {("a", a)});
    return 12;
}catch(Exception _ex_){__C__A__T__C__H__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), _ex_);
throw new Rhino.Runtime.Code.Languages.Roslyn.Core.RoslynCodeException(_ex_);}}
int DoOtherStuff(int dosa, string dosb) {try{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 23, 0, new (string, object)[] {("dosa", dosa),("dosb", dosb),("a", a)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 23, 1, new (string, object)[] {("dosa", dosa),("dosb", dosb),("a", a)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 23, 2, new (string, object)[] {("dosa", dosa),("dosb", dosb),("a", a)});
return dosa + int.Parse(dosb);}catch(Exception _ex_){__C__A__T__C__H__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), _ex_);
throw new Rhino.Runtime.Code.Languages.Roslyn.Core.RoslynCodeException(_ex_);}};
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 25, 1, new (string, object)[] {("a", a)});
DoStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 28, 1, new (string, object)[] {("a", a)});
DoStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 31, 1, new (string, object)[] {("a", a)});
var ca = new ClassStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 32, 1, new (string, object)[] {("a", a),("ca", ca)});
float cas = 0f;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 33, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas)});
ca.DoStuff(1, 2, out float caa, ref cas);
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 35, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
if (true)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 37, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
    DoStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 39, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});

    unchecked
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 41, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
        int i3 = 2147483647 + 10;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 42, 1, new (string, object)[] {("i3", i3),("a", a),("ca", ca),("cas", cas),("caa", caa)});
        Console.WriteLine(i3);
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 45, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});

    goto Remain;
}else
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 48, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
    DoStuff();
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 50, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
Remain: Console.WriteLine("remain");
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 51, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
LeftOver:
Console.WriteLine("leftover");
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 53, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
Console.WriteLine("leftover");
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 55, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa)});
int b = 9; __T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 55, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b)});
int az = 2;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 56, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
switch (b)
{
    case 1:
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 59, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
        DoStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 60, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
        break;

    default:
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 63, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
        if (true)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 64, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
            DoStuff();
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 65, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
        break;
}
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 68, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
for (int c = 0; c < 3; c++)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 68, 1, new (string, object)[] {("c", c),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 69, 1, new (string, object)[] {("c", c),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
    DoStuff();
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 71, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az)});
var obj = new object();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 72, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj)});
foreach (var c in new int[] { 1, 2, 3 })
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 72, 1, new (string, object)[] {("c", c),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 74, 1, new (string, object)[] {("c", c),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj)});
    lock (obj)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 76, 1, new (string, object)[] {("c", c),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj)});
        Console.WriteLine(a);
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 77, 1, new (string, object)[] {("c", c),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj)});
        DoStuff();
}}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 81, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj)});
int d = 0;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 82, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d)});
while (d < 3)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 82, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 84, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d)});
    DoStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 85, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d)});
    d++;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 86, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d)});
    continue;
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 89, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d)});
int e = 0;
do
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 92, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    DoStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 93, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    e++;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 95, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
}while (e < 3);
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 99, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    DoStuff();
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 102, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
        int f = 18;
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 108, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});

    if (12 is int value)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 109, 1, new (string, object)[] {("value", value),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
        DoStuff();
}    else
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 111, 1, new (string, object)[] {("value", value),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
        DoStuff();
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 113, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});

    using (var g = new StringReader("SomeData"))
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 114, 1, new (string, object)[] {("g", g),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
        DoStuff();
}}

try
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 120, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    DoStuff();
}catch (IndexOutOfRangeException)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 124, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    DoStuff();
}catch (Exception ex)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 128, 1, new (string, object)[] {("ex", ex),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    DoStuff();
}finally
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 132, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    DoStuff();
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 135, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
using (var h = new StringReader("SomeData"))
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 137, 1, new (string, object)[] {("h", h),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    DoStuff();
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 140, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
var k = Enumerable.Range(0, 10).Where(zz => zz % 2 == 0).Where(yy =>
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 140, 0, new (string, object)[] {("yy", yy),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
try{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 142, 1, new (string, object)[] {("yy", yy),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    Console.WriteLine(yy);
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 143, 1, new (string, object)[] {("yy", yy),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 143, 2, new (string, object)[] {("yy", yy),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e)});
    return true;
}catch(Exception _ex_){__C__A__T__C__H__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), _ex_);
throw new Rhino.Runtime.Code.Languages.Roslyn.Core.RoslynCodeException(_ex_);}}).ToList();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 146, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k)});
var m = Enumerable.Range(0, 10).Where((xx, vv) => xx + vv > 2);
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 147, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m)});
new List<int> { 1, 2, 3, 4 }.ForEach(i => Console.WriteLine(i));
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 149, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m)});
Params.DoStuff();
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 151, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m)});
string python2 = @"#! python2
x = a + b
";
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 155, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2)});
string python3 = @"#! python3
x = a + b
";
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 159, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3)});
string csharp = @"//#! csharp
x = (int)a + (int)b;
";
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 163, 1, new (string, object)[] {("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});
foreach (string script in new string[] { python2, python3, csharp })
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 163, 1, new (string, object)[] {("script", script),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 165, 1, new (string, object)[] {("script", script),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});
    var ctx = new ExecuteContext
    {
        
        Inputs = {
            ["a"] = 21,
            ["b"] = 21,
        },

        
        
        Outputs = {
            ["x"] = -1,
            ["j"] = -1,
        }
    };
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 181, 1, new (string, object)[] {("script", script),("ctx", ctx),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});

    RhinoCode.RunScript(script, ctx);
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 183, 1, new (string, object)[] {("script", script),("ctx", ctx),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});

    if (ctx.Outputs.TryGet("x", out int x))
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 184, 1, new (string, object)[] {("x", x),("script", script),("ctx", ctx),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});
        RhinoApp.WriteLine(x.ToString());
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 186, 1, new (string, object)[] {("script", script),("ctx", ctx),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});

    if (ctx.Outputs.TryGet("j", out int j))
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 187, 1, new (string, object)[] {("j", j),("script", script),("ctx", ctx),("a", a),("ca", ca),("cas", cas),("caa", caa),("b", b),("az", az),("obj", obj),("d", d),("e", e),("k", k),("m", m),("python2", python2),("python3", python3),("csharp", csharp)});
        RhinoApp.WriteLine(j.ToString());
}}
class ClassStuff
{
    public void DoStuff(int csa, int csb, out float a, ref float s)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 192, 0, new (string, object)[] {("csa", csa),("csb", csb),("s", s)});
try{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 194, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s)});
        int j = 42;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 195, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});
        if (true)
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 197, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});
            DoStuff();
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 200, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});
                DoStuff();
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 202, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});
                    DoStuff();
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 205, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});
                        DoStuff();
}}}}        else
{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 211, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});
            DoStuff();
}__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 213, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});

        a = 12f;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 214, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});
        s = 12f;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 216, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j)});

        var k = a + s;
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 218, 1, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j),("k", k)});
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 218, 2, new (string, object)[] {("csa", csa),("csb", csb),("s", s),("j", j),("k", k)});

        return;
}catch(Exception _ex_){__C__A__T__C__H__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), _ex_);
throw new Rhino.Runtime.Code.Languages.Roslyn.Core.RoslynCodeException(_ex_);}}
    public void DoStuff() {try{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 221, 0, System.Array.Empty<(string,object)>());
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 221, 1, System.Array.Empty<(string,object)>());
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 221, 2, System.Array.Empty<(string,object)>());
Console.WriteLine("test");}catch(Exception _ex_){__C__A__T__C__H__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), _ex_);
throw new Rhino.Runtime.Code.Languages.Roslyn.Core.RoslynCodeException(_ex_);}}
    
    
    

    
    
    
    
    
}

static class Params
{
    public static int SomeValue { get; } = 12;

    public static void DoStuff() {try{__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 238, 0, System.Array.Empty<(string,object)>());
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 238, 1, System.Array.Empty<(string,object)>());
__T_R_A_C_E__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), 238, 2, System.Array.Empty<(string,object)>());
Console.WriteLine("test");}catch(Exception _ex_){__C__A__T__C__H__(new Guid(@"a2c2d602-3592-4701-9402-338bb75514b9"), new Uri(@"file:///C:/Users/ein/gits/rhino/src4/rhino4/Plug-ins/RhinoCodePlugins/tests/test_debug_syntax.csx"), _ex_);
throw new Rhino.Runtime.Code.Languages.Roslyn.Core.RoslynCodeException(_ex_);}}}
