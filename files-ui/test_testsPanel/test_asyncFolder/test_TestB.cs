// #! csharp
// async: true
using System;
using System.Threading.Tasks;

await Task.Delay(1000);

throw new Exception("Bad");