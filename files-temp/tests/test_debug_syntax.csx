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

int DoOtherStuff(int dosa, string dosb) => dosa + int.Parse(dosb);

DoStuff();


bool TryGet(out int a)
{
    a = 42;
    return false;
}

int f = -1;
TryGet(out f);


#if !LIBRARY
DoStuff();
#endif

var ca = new ClassStuff();
float cas = 0f;
ca.DoStuff(1, 2, out float caa, ref cas);

if (true)
{
    DoStuff();

    unchecked
    {
        int i3 = 2147483647 + 10;
        Console.WriteLine(i3);
    }

    goto Remain;
}
else
    DoStuff();

Remain: Console.WriteLine("remain");
LeftOver:
Console.WriteLine("leftover");
Console.WriteLine("leftover");

int b = 9; int az = 2;
switch (b)
{
    case 1:
        DoStuff();
        break;

    default:
        if (true)
            DoStuff();
        break;
}

for (int c = 0; c < 3; c++)
    DoStuff();

var obj = new object();
foreach (var c in new int[] { 1, 2, 3 })
{
    lock (obj)
    {
        Console.WriteLine(a);
        DoStuff();
    }
}

int d = 0;
while (d < 3)
{
    DoStuff();
    d++;
    continue;
}

int e = 0;
do
{
    DoStuff();
    e++;
}
while (e < 3);


{
    DoStuff();

    {
        int f = 18;
#if DEBUG
        DoStuff();
#endif
    }

    if (12 is int value)
        DoStuff();
    else
        DoStuff();

    using (var g = new StringReader("SomeData"))
        DoStuff();
}


try
{
    DoStuff();
}
catch (IndexOutOfRangeException)
{
    DoStuff();
}
catch (Exception ex)
{
    DoStuff();
}
finally
{
    DoStuff();
}

using (var h = new StringReader("SomeData"))
{
    DoStuff();
}

var k = Enumerable.Range(0, 10).Where(zz => zz % 2 == 0).Where(yy =>
{
    Console.WriteLine(yy);
    return true;
}).ToList();

var m = Enumerable.Range(0, 10).Where((xx, vv) => xx + vv > 2);
new List<int> { 1, 2, 3, 4 }.ForEach(i => Console.WriteLine(i));

Params.DoStuff();

string python2 = @"#! python2
x = a + b
";

string python3 = @"#! python3
x = a + b
";

string csharp = @"//#! csharp
x = (int)a + (int)b;
";

foreach (string script in new string[] { python2, python3, csharp })
{
    var ctx = new ExecuteContext
    {
        // define the inputs
        Inputs = {
            ["a"] = 21,
            ["b"] = 21,
        },

        // define the outputs
        // set values to default
        Outputs = {
            ["x"] = -1,
            ["j"] = -1,
        }
    };

    // RhinoCode.RunScript(script, ctx);

    if (ctx.Outputs.TryGet("x", out int x))
        RhinoApp.WriteLine(x.ToString());

    if (ctx.Outputs.TryGet("j", out int j))
        RhinoApp.WriteLine(j.ToString());
}

class ClassStuff
{
    public void DoStuff(int csa, int csb, out float a, ref float s)
    {
        int j = 42;
        if (true)
        {
            DoStuff();

            {
                DoStuff();
                {
                    DoStuff();

                    {
                        DoStuff();
                    }
                }
            }
        }
        else
            DoStuff();

        a = 12f;
        s = 12f;

        var k = a + s;

        return;
    }

    public void DoStuff() => Console.WriteLine("test");

    // unsafe private static void ModifyFixedStorage()
    // {
    //     Point pt = new Point();

    //     fixed (int* p = &pt.x)
    //     {
    //         *p = 1;
    //     }
    // }
}

static class Params
{
    public static int SomeValue { get; } = 12;

    public static void DoStuff() => Console.WriteLine("test");
}
