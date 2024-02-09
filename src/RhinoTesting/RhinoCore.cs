using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Rhino.Testing
{
    // https://docs.nunit.org/articles/vs-test-adapter/AdapterV4-Release-Notes.html
    // https://github.com/nunit/nunit3-vs-adapter/blob/master/src/NUnitTestAdapter/AdapterSettings.cs#L143
    static class RhinoCore
    {
        static string s_systemDirectory;
        static IDisposable s_core;
        static bool s_inRhino = false;

        public static RhinoTestConfigs Configs { get; } = new RhinoTestConfigs();

        public static void Initialize()
        {
            if (s_core is null)
            {
                s_systemDirectory = Configs.RhinoSystemDir;

                AppDomain.CurrentDomain.AssemblyResolve += ResolveForRhinoAssemblies;

                LoadCore();
                LoadEto();
            }
        }

        public static void TearDown()
        {
            s_inRhino = false;
            s_core?.Dispose();
            s_core = null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void LoadCore()
        {
            s_inRhino = Process.GetCurrentProcess().ProcessName.Equals("Rhino");
            if (s_inRhino)
            {
                return;
            }

            s_core = new Rhino.Runtime.InProcess.RhinoCore();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void LoadEto()
        {
            Eto.Platform.AllowReinitialize = true;
            Eto.Platform.Initialize(Eto.Platforms.Wpf);
        }

        static readonly string[] s_subpaths = new string[]
        {
            @"Plug-ins\Grasshopper",
        };

        static Assembly ResolveForRhinoAssemblies(object sender, ResolveEventArgs args)
        {
            string name = new AssemblyName(args.Name).Name;

            string file = Path.Combine(s_systemDirectory, name + ".dll");
            if (File.Exists(file))
            {
                TestContext.WriteLine($"Loading assembly from file {file}");
                return Assembly.LoadFrom(file);
            }

            foreach (var plugin in s_subpaths)
            {
                file = Path.Combine(s_systemDirectory, plugin, name + ".dll");
                if (File.Exists(file))
                {
                    TestContext.WriteLine($"Loading plugin assembly from file {file}");
                    return Assembly.LoadFrom(file);
                }
            }

            return null;
        }
    }
}


