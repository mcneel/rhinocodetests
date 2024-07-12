#if RC8_10
using System;

using Rhino.Runtime.Code.Platform;

namespace RhinoCodePlatform.Rhino3D.Testing
{
    public sealed class ProjectReaderServer : RhinoCodePlatform.Projects.BaseRhino3DProjectServer
    {
        public override bool TryInvoke(string endpoint, InvokeContext context) => throw new NotImplementedException();
    }
}
#endif
