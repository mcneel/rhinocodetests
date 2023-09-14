using System;

using NUnit.Framework;

namespace Rhino.Testing
{
    public abstract class RhinoTestFixture
    {
        static bool _initialized = false;

        public static RhinoTestConfigs Configs = new RhinoTestConfigs();

        [OneTimeSetUp]
        public static void OneTimeSetup()
        {
            if (_initialized)
                return;

            RhinoTestSingleton.Instance.Initialize(Configs);
            _initialized = true;
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            if (_initialized)
            {
                RhinoTestSingleton.Instance.Dispose();
                _initialized = false;
            }
        }
    }
}


