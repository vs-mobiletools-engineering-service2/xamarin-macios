using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker.Steps;
using Xamarin.Bundler;

#if NET
using Mono.Linker;
#else
#if MTOUCH
using ProductException = Xamarin.Bundler.MonoTouchException;
#else
using ProductException = Xamarin.Bundler.MonoMacException;
#endif
#endif

namespace Xamarin.Linker.Steps {

	abstract public class ScanTypeReferenceStep : BaseStep {

		protected readonly List<string> lookfor;

		protected ScanTypeReferenceStep (List<string> list)
		{
			lookfor = list;
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (var module in assembly.Modules) {
				foreach (var name in lookfor) {
					if (module.HasTypeReference (name))
						Report (name, assembly);
				}
			}
		}

		protected abstract void Report (string typeName, AssemblyDefinition assembly);
	}

	public class PreLinkScanTypeReferenceStep : ScanTypeReferenceStep {

#if NET
		public PreLinkScanTypeReferenceStep () : base (LinkerConfiguration.Instance.WarnOnTypeRef)
		{
		}
#else
		public PreLinkScanTypeReferenceStep (List<string> list) : base (list)
		{
		}
#endif

		protected override void Report (string typeName, AssemblyDefinition assembly)
		{
#if NET
			var s = "warning MT1502: " + String.Format (Errors.MX1502, typeName, assembly);
			Context.LogMessage (MessageContainer.CreateInfoMessage (s));
#else
			ErrorHelper.Show (new ProductException (1502, false, Errors.MX1502, typeName, assembly));
#endif
		}
	}

	public class PostLinkScanTypeReferenceStep : ScanTypeReferenceStep {

#if NET
		public PostLinkScanTypeReferenceStep () : base (LinkerConfiguration.Instance.WarnOnTypeRef)
		{
		}
#else
		public PostLinkScanTypeReferenceStep (List<string> list) : base (list)
		{
		}
#endif

		protected override void Report (string typeName, AssemblyDefinition assembly)
		{
#if NET
			var s = "warning MT1503: " + String.Format (Errors.MX1503, typeName, assembly);
			Context.LogMessage (MessageContainer.CreateInfoMessage (s));
#else
			ErrorHelper.Show (new ProductException (1503, false, Errors.MX1503, typeName, assembly));
#endif
		}
	}
}
