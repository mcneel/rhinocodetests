// async:true
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

var t = new Stopwatch();
t.Start();


var ts = new List<Task>();

ts.Add(Task.Run(() => DoStuff(0, 0)));
ts.Add(Task.Run(() => DoStuff(1, 2000)));
ts.Add(Task.Run(() => DoStuff(2, 1000)));

Task.WaitAll(ts.ToArray());

t.Stop();
Console.WriteLine($"All: {t.Elapsed}");

void DoStuff(int thread, int sleep)
{
	var sw = new Stopwatch();
	sw.Start();

	// Thread.Sleep(sleep);
    Console.WriteLine("Do Stuff");

	// Thread.Sleep(sleep);
    Console.WriteLine("Do Stuff");

	sw.Stop();
    Console.WriteLine($"{thread}: {sw.Elapsed} of {sleep}");
    Console.WriteLine("Do Stuff");
}