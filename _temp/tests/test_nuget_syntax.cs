#r "nuget: Microsoft.CodeAnalysis.Common, 4.3.0"
#r "nuget: Microsoft.CodeAnalysis.CSharp, 4.3.0"
#r "nuget: Microsoft.CodeAnalysis.CSharp.Features, 4.3.0"
#r "nuget: Microsoft.CodeAnalysis.CSharp.Scripting, 4.3.0"
#r "nuget: Microsoft.CodeAnalysis.CSharp.Workspaces, 4.3.0"

using System;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

string code = "using Rhino.Geometry;\r\nvar p = new ";
int position = 37;

SyntaxTree tree = CSharpSyntaxTree.ParseText(code);

//CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
//var node = root.FindToken(position).Parent;

Console.WriteLine(tree);
