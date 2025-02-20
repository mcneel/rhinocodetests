// #! csharp
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;

Sphere geometry_in_memory_with_no_document = new Sphere(Point3d.Origin, 10);

string png = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), "Downloads", "test.png");

var doc = Rhino.RhinoDoc.CreateHeadless(string.Empty);

doc.Objects.AddSphere(geometry_in_memory_with_no_document);

var v = doc.Views.Add("Thumbnail",
            Rhino.Display.DefinedViewportProjection.Perspective,
            new Rectangle(0,0, 400, 400),
            false);

var vcs = new ViewCaptureSettings(v, new Size(100, 100), 2.0);
var bmp = ViewCapture.CaptureToBitmap(vcs);
bmp.Save(png, ImageFormat.Png);

