using System;
using System.Collections;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

using RhinoCodePlatform.Rhino3D.GH;

namespace RhinoCodePlatform.Rhino3D.Testing
{
    public sealed class ProgressReporterAttribs : GH_ComponentAttributes, IScriptAttribute
    {
        public bool Pass { get; private set; } = false;

        public ProgressReporterAttribs(IGH_Component component) : base(component) { }

        public void ShowProgress(float progress, string message)
        {
            Pass |= progress > 0f;
        }
    }
}
