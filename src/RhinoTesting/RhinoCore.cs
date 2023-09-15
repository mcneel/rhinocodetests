using System;
using System.IO;
using System.Reflection;

namespace Rhino.Testing
{
    // https://docs.nunit.org/articles/vs-test-adapter/AdapterV4-Release-Notes.html
    // https://github.com/nunit/nunit3-vs-adapter/blob/master/src/NUnitTestAdapter/AdapterSettings.cs#L143
    static class RhinoCore
    {
        static string _systemDirectory;
        static IDisposable _core;

        public static RhinoTestConfigs Configs = new RhinoTestConfigs();

        public static void Initialize()
        {
            if (_core is null)
            {
                _systemDirectory = Configs.RhinoSystemDir;

                RhinoInside.Resolver.Initialize();
                RhinoInside.Resolver.RhinoSystemDirectory = _systemDirectory;
                AppDomain.CurrentDomain.AssemblyResolve += ResolveForRhinoAssemblies;
                LoadCore();
                LoadEto();
            }
        }

        public static void TearDown()
        {
            _core?.Dispose();
            _core = null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void LoadCore()
        {
            _core = new Rhino.Runtime.InProcess.RhinoCore();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void LoadEto()
        {
            Eto.Platform.AllowReinitialize = true;
            Eto.Platform.Initialize(Eto.Platforms.Wpf);
        }

        static readonly string[] s_pluginpaths = new string[]
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
    }
}


