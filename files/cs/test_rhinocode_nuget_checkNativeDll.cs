// r "Mono.Cecil.dll"

using System;
using Mono.Cecil;

bool test = true;
try
{
    ModuleDefinition.ReadModule(@"C:\Users\ein\.nuget\packages\activiz.net.x64\5.8.0\lib\net20\msvcr90.dll");
    test = false;
}
catch(BadImageFormatException)
{
    test = true;
}

result = test;
