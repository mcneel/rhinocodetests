// #! csharp
using System;

// special vars should be available when project is open
// and this code belongs to the project
// and NOT available otherwise
Console.WriteLine(__rhino_command__ ?? "None");
Console.WriteLine(__rhino_doc__);
Console.WriteLine(__rhino_runmode__);
Console.WriteLine(__is_interactive__);