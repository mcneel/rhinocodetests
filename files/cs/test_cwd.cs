using System;
using System.IO;

string cwd = Directory.GetCurrentDirectory();
result = cwd.EndsWith("Debug")
      || cwd.EndsWith("Release")
      || cwd.EndsWith("System")
      || cwd.EndsWith("net7.0-windows")
      || cwd.EndsWith("net48");
