using System;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;

using Rhino.Runtime.Code.Execution;

namespace Rhino.Runtime.Code.Tests
{
    [TestFixture]
    public class BuildOptionTests
    {
        [Test]
        public void TestBuildOption_CompileGuard_Remove()
        {
            var opts = new LibraryBuildOptions();
            opts.CompileGuards.Remove(LibraryBuildOptions.DEFINE_LIBRARY);
            Assert.IsFalse(opts.CompileGuards.Contains(LibraryBuildOptions.DEFINE_LIBRARY));
        }

        [Test]
        public void TestBuildOption_CompileGuard_Remove_Id()
        {
            var opts = new LibraryBuildOptions();
            opts.CompileGuards.Remove(LibraryBuildOptions.DEFINE_LIBRARY.Identifier);
            Assert.IsFalse(opts.CompileGuards.Contains(LibraryBuildOptions.DEFINE_LIBRARY.Identifier));
        }
    }
}
