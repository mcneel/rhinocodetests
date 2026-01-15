using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Execution;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Platform;
using Rhino.Runtime.Code.Projects;

using Rhino.Runtime.Code.Testing;

namespace RhinoCodePlatform.Rhino3D.Testing
{
    public sealed class ProjectReaderServer : RhinoCodePlatform.Projects.BaseRhino3DProjectServer
    {
        public override IEnumerable<Param> GetArguments(LanguageSpec languageSpec) => Enumerable.Empty<Param>();

        public override IEnumerable<Param> GetArguments(IProject project, Code code) => Enumerable.Empty<Param>();

        public override bool TryPrepareContext(LanguageSpec languageSpec, RunContext context) => false;

        public override bool TryPrepareContext(IProject project, Code code, RunContext context) => false;

        public override bool TryPrepareContext(IStoredTestsLibrary tests, StoredTest test, RunContext context) => false;

        public override bool TryInvoke(string endpoint, InvokeContext context) => throw new NotImplementedException();
    }
}
