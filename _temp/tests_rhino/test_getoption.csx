using System;
using Rhino;
using Rhino.Input.Custom;

var gopt = new GetOption();

gopt.SetCommandPrompt("Select Debug Action");
gopt.AddOption("Continue", "C");
gopt.AddOption("StepOver", "O");
gopt.AddOption("StepIn", "I");
gopt.AddOption("StepOut", "U");
gopt.AddOption("Disconnect", "D");
gopt.AddOption("Stop", "P");
gopt.AcceptNothing(false);
gopt.EnableTransparentCommands(false);

gopt.Get();
Console.WriteLine(gopt.Option().LocalName);

gopt.Get();
Console.WriteLine(gopt.Option().LocalName);

gopt.Get();
Console.WriteLine(gopt.Option().LocalName);