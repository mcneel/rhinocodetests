using System;
using System.IO;

string cwd = Directory.GetCurrentDirectory();
result = cwd.EndsWith("Debug")
      || cwd.EndsWith("Release")
      || cwd.EndsWith("System")
      || cwd.EndsWith("net7.0-windows") // rhino 8.x
      || cwd.EndsWith("net9.0-windows") // rhino 9.x
      || cwd.EndsWith("net48");
