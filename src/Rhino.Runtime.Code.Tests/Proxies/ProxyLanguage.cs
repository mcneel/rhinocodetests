using System;
using System.Linq;
using System.Collections.Generic;

using Rhino.Runtime.Code;
using Rhino.Runtime.Code.Diagnostics;
using Rhino.Runtime.Code.Languages;
using Rhino.Runtime.Code.Storage;
using Rhino.Runtime.Code.Execution;

namespace RhinoCodePlatform.Projects.Proxies
{
    public sealed class ProxyCode : Code
    {
        public ProxyCode(ILanguage lang) : base(lang) { }

        public override bool IsCached() => false;
        public override bool IsCached(BuildContext context) => false;
        public override void ExpireCache() { }

        protected override void BeginStreams(RunContext context) { }
        protected override void EndStreams(ResetStreamPolicy resetPolicy) { }

        protected override void BeginTrace(IExecTracer tracer) { }
        protected override void EndTrace() { }

        protected override void Compile(BuildContext context)
        {
            switch ((string)Text)
            {
                case "<compile-exception>":
                    throw new CompileException(new Diagnostic(DiagnosticSeverity.Error, ""));
            }
        }

        protected override void Execute(RunContext context)
        {
            switch ((string)Text)
            {
                case "<execute-exception>":
                    throw new ExecuteException(string.Empty);
            }
        }
    }

    public sealed class ProxyLanguage : Language<Code>
    {
        public override LanguageIdentity Id { get; } = new LanguageIdentity("ProxyLanguage", "rhinocode.tests.proxyLang", new Version(1, 0, 0));

        #region Not Necessary
        public override Code CreateCode() => new ProxyCode(this);
#if RC9_0
        public override IEnumerable<ILanguageSpecifier> Specifiers { get; } = Enumerable.Empty<ILanguageSpecifier>();
        public override LanguageStoredLibrary CreateLibrary(Uri uri) => throw new NotImplementedException();
#else
        public override IEnumerable<IStorageFilter> StorageFilters { get; } = Enumerable.Empty<IStorageFilter>();
        public override LanguageLibrary CreateLibrary(Uri uri) => throw new NotImplementedException();
#endif
        public override LanguageSourceLibrary CreateLibrary(string name) => throw new NotImplementedException();
        #endregion
    }
}
