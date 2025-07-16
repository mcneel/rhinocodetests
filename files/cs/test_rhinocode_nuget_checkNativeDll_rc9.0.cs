# r "nuget: Activiz.NET.x64, 5.8.0"
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Environments;
using NUnit.Framework;

string home = Path.GetFullPath(Environment.ExpandEnvironmentVariables("%USERPROFILE%"));
var lib = $@"{home}\.nuget\packages\activiz.net.x64\5.8.0\lib\net20\msvcr90.dll";
result = !EnvironExtensions.IsManagedAssembly(lib, out string _, out Version _);