#r "Eto.dll"

using System;
using Rhino;
using EF = Eto.Forms;

var ofd = new EF.OpenFileDialog();
// ofd.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow);

Uri d = ofd.Directory;
// Console.WriteLine(d);
