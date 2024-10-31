#pragma ref RhinoCommon
using System;

using System.Diagnostics;

using Rhino;

var doc = RhinoDoc.ActiveDoc;
var mesh = new Rhino.Geometry.Mesh();

mesh.Vertices.Add(0.0, 0.0, 10.0); //0
mesh.Vertices.Add(1.0, 0.0, 1.0); //1
mesh.Vertices.Add(2.0, 0.0, 1.0); //2
mesh.Vertices.Add(3.0, 0.0, 0.0); //3
mesh.Vertices.Add(0.0, 1.0, 1.0); //4
mesh.Vertices.Add(1.0, 1.0, 2.0); //5
mesh.Vertices.Add(2.0, 1.0, 1.0); //6
mesh.Vertices.Add(3.0, 1.0, 0.0); //7
mesh.Vertices.Add(0.0, 2.0, 1.0); //8
mesh.Vertices.Add(1.0, 2.0, 1.0); //9
mesh.Vertices.Add(2.0, 2.0, 1.0); //10
mesh.Vertices.Add(3.0, 2.0, 1.0); //11

mesh.Faces.AddFace(0, 1, 5, 4);
mesh.Faces.AddFace(1, 2, 6, 5);
mesh.Faces.AddFace(2, 3, 7, 6);
mesh.Faces.AddFace(4, 5, 9, 8);
mesh.Faces.AddFace(5, 6, 10, 9);
mesh.Faces.AddFace(6, 7, 11, 10);
mesh.Normals.ComputeNormals();
mesh.Compact();

Console.WriteLine($"PartitionCount: {mesh.PartitionCount}");

if (doc.Objects.AddMesh(mesh) != Guid.Empty)
    doc.Views.Redraw();

var m = new MyScript();
m.Main();

public class MyScript {
	public void Main() {
        Debugger.Break();
        int m = this.Value;
        Console.WriteLine(m);

		Console.WriteLine("Testing C#");
        var inputString = Console.ReadLine();
        Console.WriteLine($"echo: {inputString}");
        
        Console.WriteLine("Start Wait");
        // Thread.Sleep(5000);
        Console.WriteLine("Stop Wait");

        Debugger.Log(0, "", "Testing debugger...\n");
    }
    
    public int Value => 12;
}
