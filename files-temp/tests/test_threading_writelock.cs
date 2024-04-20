// async: true
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

var w = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

Console.WriteLine("Begin: -----");

void TestLock()
{
    if (w.IsWriteLockHeld)
    {
        Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} -> True");
        return;
    }

    w.EnterWriteLock();
    Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId} -> Locked");
    Thread.Sleep(3000);
    w.ExitWriteLock();
}


var ts = new List<Task>();

ts.Add(Task.Run(() => TestLock()));
ts.Add(Task.Run(() => TestLock()));
ts.Add(Task.Run(() => TestLock()));

Task.WaitAll(ts.ToArray());
