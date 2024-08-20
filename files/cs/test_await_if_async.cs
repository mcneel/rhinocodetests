// async: true
using System;
using System.Threading;
using System.Threading.Tasks;

int main = Thread.CurrentThread.ManagedThreadId;

async Task<bool> Compute()
{
    bool test = true;
    test &= Thread.CurrentThread.ManagedThreadId == main;

    await Task.Run(() =>
    {
        test &= Thread.CurrentThread.ManagedThreadId != main;
        Thread.Sleep(2000);
    });

    test &= Thread.CurrentThread.ManagedThreadId == main;
    return test;
}

bool test = true;

if (await Compute())
{
    test &= true;
}

test &= Thread.CurrentThread.ManagedThreadId == main;

// Console.WriteLine(test);
result = test;