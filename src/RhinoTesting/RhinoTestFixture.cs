using System;

using NUnit.Framework;

namespace Rhino.Testing
{
    public abstract class RhinoTestFixture
    {
        public static RhinoTestConfigs Configs = new RhinoTestConfigs();

        [OneTimeSetUp]
        public static void OneTimeSetup()
        {
            RhinoCore.Initialize(Configs);
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            RhinoCore.TearDown();
        }
    }
}


