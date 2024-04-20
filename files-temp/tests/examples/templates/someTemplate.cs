#r "nuget: Pastel, 3.0.0"
#r "netstandard"
#r "System.Drawing"
#r "nuget: ColorConsole, 1.0.1"

using System;
using System.Drawing;

using Pastel;
using ColorConsole;

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