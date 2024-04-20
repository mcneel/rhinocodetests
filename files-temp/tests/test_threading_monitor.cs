// async: true
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

object lockObject = new object();

void TestLock()
{
    // Attempt to enter the lock without blocking.
    bool lockTaken = Monitor.TryEnter(lockObject);

    try
    {
        if (lockTaken)
        {
            // This thread was the first to enter and will execute the logic.
            Console.WriteLine("Thread {0} is executing the logic.", Thread.CurrentThread.ManagedThreadId);

            // Simulate some work
            Thread.Sleep(3000);
        }
        else
        {
            Console.WriteLine("Thread {0} is waiting.", Thread.CurrentThread.ManagedThreadId);
            lock(lockObject) { }
        }
    }
    finally
    {
        // Only release the lock if this thread acquired it.
        if (lockTaken)
        {
            Monitor.Exit(lockObject);
        }
    }

    Console.WriteLine("Thread {0} is exiting.", Thread.CurrentThread.ManagedThreadId);
}


var ts = new List<Task>();

ts.Add(Task.Run(() => TestLock()));
ts.Add(Task.Run(() => TestLock()));
ts.Add(Task.Run(() => TestLock()));

Task.WaitAll(ts.ToArray());
