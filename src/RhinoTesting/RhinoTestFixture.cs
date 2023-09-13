using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using NUnit.Framework;

namespace Rhino.Testing
{
    public abstract class RhinoTestFixture
    {
        [OneTimeSetUp]
        public static void OneTimeSetup()
        {
            SetupConfigs();
            RhinoTestSingleton.Instance.Initialize();
        }

        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            RhinoTestSingleton.Instance.Dispose();
        }

        static readonly Assembly s_assembly = typeof(RhinoTestFixture).Assembly;
        static readonly string SETTINGS_FILE = $"{typeof(RhinoTestFixture).Name}.xml";

        static void SetupConfigs()
        {
            string rhinoSystemDir = string.Empty;
            string settingsDir = Path.GetDirectoryName(s_assembly.Location);
            string settingsFile = Path.Combine(settingsDir, SETTINGS_FILE);

            if (File.Exists(settingsFile))
            {
                XDocument xml = XDocument.Load(settingsFile);
                rhinoSystemDir = xml.Descendants("RhinoSystemDirectory").FirstOrDefault()?.Value ?? null;
            }

            RhinoInside.Resolver.Initialize();

            if (!string.IsNullOrEmpty(rhinoSystemDir))
            {
                TestContext.WriteLine("RhinoSystemDir is: " + rhinoSystemDir + ".");
                RhinoInside.Resolver.RhinoSystemDirectory = rhinoSystemDir;
            }
        }
    }
}


