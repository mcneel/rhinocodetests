// #! csharp
using System;

// special vars should be available when project is open
// and this code belongs to the project
// and NOT available otherwise
Console.WriteLine($"{__rhino_command__}\n");
Console.WriteLine($"{__rhino_doc__}\n");
Console.WriteLine($"{__rhino_runmode__}\n");
Console.WriteLine($"{__is_interactive__}\n");