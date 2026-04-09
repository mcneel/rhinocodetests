// #! csharp
using System;
using Rhino.Runtime.Code.Execution;

result = false;

try
{
    Environment.Exit(-42);
}
catch(ExitException ee)
{
    result = ee.ExitCode == -42;
}
