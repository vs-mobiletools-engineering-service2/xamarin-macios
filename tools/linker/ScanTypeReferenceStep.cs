using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker.Steps;
using Xamarin.Bundler;

#if NET
using Mono.Linker;
#else
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
					if (IsReferenced (module, name))
						Report (name, assembly);
				}
			}
		}

		protected abstract bool IsReferenced (ModuleDefinition module, string name);

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

		protected override bool IsReferenced (ModuleDefinition module, string name)
		{
			return module.HasTypeReference (name);
		}

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

		protected override bool IsReferenced (ModuleDefinition module, string name)
		{
			if (!module.TryGetTypeReference (name, out var tr))
				return false;
			// it might be there (and not cleaned) until it's saved back to disk
			// but it can't resolve anymore (since it's removed from the actual assembly)
			var td = tr.Resolve ();
			if (td == null)
				return false;
			// and, if it was (cache) then we can ask if it was marked (since we're post mark)
			return Annotations.IsMarked (td);
		}

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
