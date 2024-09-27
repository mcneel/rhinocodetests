using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;

namespace RhinoCodePlatform.Rhino3D.Testing.Client
{
    [Serializable]
    [XmlRoot("Settings")]
    public sealed class TestClientConfigs
    {
        public static T Deserialize<T>(string settingsFile) => Deserialize<T>(new XmlSerializer(typeof(T)), settingsFile);

        public static T Deserialize<T>(XmlSerializer serializer, string settingsFile)
        {
            if (serializer is null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            using FileStream fstream = new(settingsFile, FileMode.Open);
            using XmlReader reader = XmlReader.Create(fstream);
            return (T)serializer.Deserialize(reader);
        }

        public static TestClientConfigs Current { get; } = new TestClientConfigs();

        [XmlElement("RhinoSystemDirectory")]
        public string RhinoSystemDir { get; set; } = string.Empty;

        static TestClientConfigs()
        {
            string cfgFile = GetConfigsFile();

            if (File.Exists(cfgFile))
            {
                Current = Deserialize<TestClientConfigs>(new XmlSerializer(typeof(TestClientConfigs)), cfgFile);

                if (Path.IsPathRooted(Current.RhinoSystemDir))
                {
                    return;
                }

                Current.RhinoSystemDir = Path.GetFullPath(
                        Path.Combine(Path.GetDirectoryName(cfgFile), Current.RhinoSystemDir)
                    );
            }
        }

        static string GetConfigsFile()
        {
            Assembly s_assembly = typeof(TestClientConfigs).Assembly;
            string s_settingsFileName = $"{nameof(TestClient)}.Configs.xml";
            string settingsDir = Path.GetDirectoryName(s_assembly.Location);
            return Path.Combine(settingsDir, s_settingsFileName);
        }
    }
}
