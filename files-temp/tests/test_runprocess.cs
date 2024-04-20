using System;
using System.Threading;
using System.Diagnostics;
using Rhino.Runtime.Code;

bool passed = false;
void CheckOutput(object sender, DataReceivedEventArgs e)
{
    if (string.IsNullOrEmpty(e.Data))
        return;

    if (passed)
        return;
    

    Console.WriteLine("PASS");
    passed = true;
}

// this should not print anything
using (var ts = new CancellationTokenSource(10))
{
    ProcessResult res = 
        RhinoCode.RunProcess("powershell.exe",
                             $"-NoLogo -Command \"ls\"",
                             ts.Token);
    
    Console.WriteLine(string.IsNullOrEmpty(res.Output) ? "PASS" : "FAIL");
}


// this should print something
using (var ts = new CancellationTokenSource())
{
    ProcessResult res = 
        RhinoCode.RunProcess("powershell.exe",
                             $"-NoLogo -Command \"ls\"",
                             ts.Token);

    Console.WriteLine(!string.IsNullOrEmpty(res.Output) ? "PASS" : "FAIL");
}

// this should receive something on output
using (var ts = new CancellationTokenSource())
{
    ProcessResult res = 
        RhinoCode.RunProcess("powershell.exe",
                             $"-NoLogo -Command \"ls\"",
                             ts.Token,
                             CheckOutput,
                             (sender, e) => Console.WriteLine(e.Data));
}

