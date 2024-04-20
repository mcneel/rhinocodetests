using System;
using System.Diagnostics;
using Rhino;

int radius = 100;
bool sts = Rhino.RhinoApp.RunScript($"-Circle 0,0,0 {radius}", true);

if (sts)
    RhinoDoc.ActiveDoc.Views.Redraw();
