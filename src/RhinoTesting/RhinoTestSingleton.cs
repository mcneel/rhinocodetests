using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhino.Testing
{
    class RhinoTestSingleton : IDisposable
    {
        static string _systemDirectory;
        IDisposable _core;

        public static RhinoTestSingleton Instance { get; }

        static RhinoTestSingleton() => Instance = new RhinoTestSingleton();

        public void Initialize(RhinoTestConfigs configs)
        {
            _systemDirectory = configs.RhinoSystemDir;

            RhinoInside.Resolver.Initialize();
            RhinoInside.Resolver.RhinoSystemDirectory = _systemDirectory;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveForRhinoAssemblies;
            LoadCore();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        void LoadCore()
        {
            if (_core is null)
                _core = new Rhino.Runtime.InProcess.RhinoCore();
        }

        static string[] s_pluginpaths = new string[]
        {
            @"Plug-ins\Grasshopper",
        };

        static Assembly ResolveForRhinoAssemblies(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;

            foreach (var plugin in s_pluginpaths)
            {
                string file = Path.Combine(_systemDirectory, plugin, name + ".dll");
                if (File.Exists(file))
                    return Assembly.LoadFrom(file);
            }

            return null;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _core?.Dispose();
            }
        }
    }
}


