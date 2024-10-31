// r "C:\TeklaStructures\2022.0\bin\Tekla.Structures.dll"
// r "C:\TeklaStructures\2022.0\bin\Tekla.Structures.Plugins.dll"
// r "C:\TeklaStructures\2022.0\bin\Tekla.Structures.Model.dll"

using System;
using Tekla.Structures;

var model = new Model();
Console.WriteLine($"{model.GetInfo().NorthDirection}");