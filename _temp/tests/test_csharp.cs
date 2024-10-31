// top level statements as a C# script
using System;
using System.Linq;
using System.Collections.Generic;

int k = 12;
// throw new Exception("psst");

var m = new MyScript();
m.Main();

Console.WriteLine("Test");

int f = 12;
f = 8;

Guid g = Guid.NewGuid();
Console.WriteLine(g);

Uri u = new Uri("http://test.com");

var ml = new List<int> { 1, 2, 3, 4, 5, 6 };

var s = ml.Where(x => x % 2 == 0).ToList();

Console.WriteLine(s);

public class MyScript
{
    public void Main()
    {
        int m = this.Value;
        Console.WriteLine(m);
        Console.WriteLine("Testing C#");
		throw new Exception("psst");
    }

    public int Value => 12;
}
