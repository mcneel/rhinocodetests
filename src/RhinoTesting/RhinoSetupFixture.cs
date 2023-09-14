using System;

using NUnit.Framework;

namespace Rhino.Testing
{
    public abstract class RhinoSetupFixture
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            RhinoCore.Initialize();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            RhinoCore.TearDown();
        }
    }
}


