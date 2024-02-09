using System;

using NUnit.Framework;

namespace Rhino.Testing.Fixtures
{
    [TestFixture]
    public abstract class RhinoTestFixture
    {
        public static RhinoTestConfigs Configs => RhinoCore.Configs;
    }
}
