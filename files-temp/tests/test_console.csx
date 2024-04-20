#r "nuget: Pastel, 4.1.0"
#r "nuget: ColorConsole, 1.0.1"

using System;
using System.IO;
using System.Drawing;

using Pastel;
using ColorConsole;

using Rhino.PlugIns;

// system console
Console.BackgroundColor = ConsoleColor.Blue;
Console.WriteLine($"{Console.BackgroundColor}");
Console.WriteLine("White on blue!");
Console.BackgroundColor = ConsoleColor.Black;

// pastel
Console.WriteLine("This is in Red".Pastel(Color.Red));


// colorconsole
var console = new ConsoleWriter();
console.Write("Be seeing you in Yellow!\n", ConsoleColor.Yellow);

// emojis
Console.WriteLine("😍");
