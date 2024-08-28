// #! csharp
using System;
using Eto.Forms;
using Eto.Drawing;
using Rhino;

Rhino.RhinoApp.ClearCommandHistoryWindow();

Test.Print();
Console.WriteLine("TEST");

int counter = 0;

var form = new Eto.Forms.Form();
form.Owner = Rhino.UI.RhinoEtoApp.MainWindow;
form.Size = new Eto.Drawing.Size(400, 400);
form.Title = "CSharp";
form.MouseMove += (s, e) => {
    Console.WriteLine($"csharp test: {counter}");
    counter++;
};
form.Show();


static class Test
{
    public static void Print() => Console.WriteLine("Test.Print");
}