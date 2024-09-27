using System;
using System.Reflection;

using RhinoInside;

namespace RhinoCodePlatform.Rhino3D.Testing.Client
{
    static class TestClient
    {
        static TestClient()
        {
            Resolver.Initialize(TestClientConfigs.Current.RhinoSystemDir);
            Console.WriteLine($"Loading Rhino @ {TestClientConfigs.Current.RhinoSystemDir}");
        }

        [System.STAThread]
        static void Main(string[] args)
        {
            try
            {
                string arg = args[0];
                Console.WriteLine($"Running test case {arg}");
                foreach (MethodInfo method in typeof(TestCases).GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    if (method.Name == arg)
                        Environment.Exit((int)method.Invoke(null, Array.Empty<object>()));
                }

                Environment.Exit(-1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
