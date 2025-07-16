#r "nuget: Activiz.NET.x64, 5.8.0"
#r "Mono.Cecil.dll"

using System;
using System.IO;

using Mono.Cecil;

bool test = true;
try
{
    string home = Path.GetFullPath(Environment.ExpandEnvironmentVariables("%USERPROFILE%"));

    ModuleDefinition.ReadModule($@"{home}\.nuget\packages\activiz.net.x64\5.8.0\lib\net20\msvcr90.dll");

    test = false;
}
catch (BadImageFormatException)
{
    test = true;
}

result = test;
