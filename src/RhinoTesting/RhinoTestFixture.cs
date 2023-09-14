using System;

namespace Rhino.Testing
{
    public abstract class RhinoTestFixture
    {
        public static RhinoTestConfigs Configs => RhinoCore.Configs;
    }
}
