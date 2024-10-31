#r "nuget: Microsoft.CodeAnalysis.CSharp.Workspaces, 4.8.0"

using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

string code = "using Rhino.Geometry;\r\nvar p = new ";
int position = 37;

SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
//CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
//var node = root.FindToken(position).Parent;

Console.WriteLine(tree);