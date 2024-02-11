using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

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

        public static void Initialize()
        {
            if (s_core is null)
            {
                s_systemDirectory = Configs.Current.RhinoSystemDir;

                AppDomain.CurrentDomain.AssemblyResolve += ResolveForRhinoAssemblies;

                LoadCore();
                LoadEto();

                LoadPlugins();
            }
        }

        public static void TearDown()
        {
            if (s_core is IDisposable)
            {
                DisposeCore();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void LoadCore()
        {
            s_inRhino = Process.GetCurrentProcess().ProcessName.Equals("Rhino");
            if (s_inRhino)
            {
                return;
            }

            s_core = new Rhino.Runtime.InProcess.RhinoCore();

            // configure Rhino
            Rhino.RhinoApp.SendWriteToConsole = true;
            if (!Rhino.Runtime.HostUtils.CheckForRdk(false, false))
            {
                Rhino.Runtime.HostUtils.InitializeRhinoCommon_RDK();
            }
            Rhino.Runtime.HostUtils.OnExceptionReport += (source, ex) =>
            {
                TestContext.WriteLine($"Error: {ex}");
            };
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void LoadEto()
        {
            Eto.Platform.AllowReinitialize = true;
            Eto.Platform.Initialize(Eto.Platforms.Wpf);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void LoadPlugins()
        {
            TestContext.WriteLine("Loading grasshopper (Headless)");

            string ghPlugin = Path.Combine(s_systemDirectory, @"Plug-ins\Grasshopper", "GrasshopperPlugin.rhp");

            Rhino.PlugIns.PlugIn.LoadPlugIn(ghPlugin, out Guid _);
            object ghObj = Rhino.RhinoApp.GetPlugInObject("Grasshopper");
            if (ghObj?.GetType().GetMethod("RunHeadless") is MethodInfo runHeadLess)
                runHeadLess.Invoke(ghObj, null);
            else
                TestContext.WriteLine("Failed loading grasshopper (Headless)");
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        static void DisposeCore()
        {
            s_inRhino = false;

            ((Rhino.Runtime.InProcess.RhinoCore)s_core).Dispose();
            s_core = null;
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

            TestContext.WriteLine($"Could not find assembly {name}");
            return null;
        }
    }
}


