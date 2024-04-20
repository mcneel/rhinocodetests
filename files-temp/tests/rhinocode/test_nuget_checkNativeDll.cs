// r "Mono.Cecil.dll"

using System;
using Mono.Cecil;

try
{
    ModuleDefinition.ReadModule(@"C:\Users\ein\.nuget\packages\activiz.net.x64\5.8.0\lib\net20\msvcr90.dll");
    Console.WriteLine("dotnet dll");
}
catch(BadImageFormatException)
{
    Console.WriteLine("native dll");
}
