using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Rhino.Testing
{
    public class RhinoTestConfigs
    {
        static readonly string SETTINGS_FILE = $"{typeof(RhinoTestConfigs).Name}.xml";
        static readonly Assembly s_assembly = typeof(RhinoTestConfigs).Assembly;
        readonly XDocument _xml;

        public string RhinoSystemDir { get; private set; }
        public string SettingsDir { get; private set; }
        public string SettingsFile { get; private set; }

        public bool TryGetConfig<T>(string name, out T value)
        {
            value = default;

            object v = _xml.Descendants(name).FirstOrDefault()?.Value;

            if (!(v is null)
                    && typeof(T).IsAssignableFrom(v.GetType()))
            {
                value = (T)v;
                return true;
            }

            return false;
        }

        public RhinoTestConfigs()
        {
            RhinoSystemDir = string.Empty;
            SettingsDir = Path.GetDirectoryName(s_assembly.Location);
            SettingsFile = Path.Combine(SettingsDir, SETTINGS_FILE);

            if (File.Exists(SettingsFile))
            {
                _xml = XDocument.Load(SettingsFile);
                RhinoSystemDir = _xml.Descendants("RhinoSystemDirectory").FirstOrDefault()?.Value ?? null;
                if (!Path.IsPathRooted(RhinoSystemDir))
                {
                    RhinoSystemDir = Path.GetFullPath(Path.Combine(SettingsDir, RhinoSystemDir));
                }
            }
        }
    }
}
