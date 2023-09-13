using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

using NUnit.Framework;

namespace Rhino.Testing
{
    class RhinoTestSingleton : IDisposable
    {
        IDisposable _core;

        public static RhinoTestSingleton Instance { get; }

        static RhinoTestSingleton() => Instance = new RhinoTestSingleton();

        public void Initialize()
        {
            if (_core is null)
                _core = new Rhino.Runtime.InProcess.RhinoCore();
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


